using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

// Acesso conforme ProjectAccess: dono e membros leem; só o dono altera, arquiva e exclui.
public class ProjectService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<ProjectService> logger, ILiveNotifier? live = null)
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        await EnsureOwnSpaceAsync(request.SpaceId, ct);

        var project = new Project(request.Name, request.Description, currentUser.UserId, request.SpaceId);

        db.Projects.Add(project);
        db.TaskTypes.AddRange(TaskType.Defaults(project.Id));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectCreated {ProjectId} {UserId}", project.Id, project.OwnerId);
        TaskFlowMetrics.ProjectsCreated.Add(1);

        return await ResponseAsync(project, ct);
    }

    // Diretório de projetos: escopo (aba), busca por nome, espaço e limite opcionais.
    public async Task<IReadOnlyList<ProjectSummaryResponse>> ListAsync(
        ProjectScope scope = ProjectScope.All,
        string? search = null,
        int? limit = null,
        Guid? spaceId = null,
        CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var query = db.AccessibleProjects(userId).AsNoTracking();

        query = scope switch
        {
            ProjectScope.Mine => query.Where(p => p.ArchivedAt == null && p.OwnerId == userId),
            ProjectScope.Archived => query.Where(p => p.ArchivedAt != null),
            _ => query.Where(p => p.ArchivedAt == null)
        };

        if (spaceId is { } space)
            query = query.Where(p => p.SpaceId == space);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower + Contains vira LIKE sem diferenciar maiúsculas, sem depender de função específica do PostgreSQL.
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        if (limit is { } take)
            query = query.Take(take);

        // Contagens, dono e espaço calculados no banco (subconsultas), sem carregar tarefas nem usuários.
        return await query
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
                db.Users.Where(u => u.Id == p.OwnerId).Select(u => u.Name).First(),
                p.SpaceId,
                db.Spaces.Where(s => s.Id == p.SpaceId).Select(s => s.Name).FirstOrDefault(),
                p.Status,
                p.StatusUpdatedAt,
                p.ArchivedAt != null,
                p.CreatedAt,
                db.Tasks.Count(t => t.ProjectId == p.Id),
                db.Tasks.Count(t => t.ProjectId == p.Id && t.Status == TaskItemStatus.Done)))
            .ToListAsync(ct);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        await ResponseAsync(await db.FindAccessibleProjectAsync(currentUser.UserId, id, ct), ct);

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Update(request.Name, request.Description);

        if (request.SpaceId != project.SpaceId)
        {
            await EnsureOwnSpaceAsync(request.SpaceId, ct);
            project.MoveToSpace(request.SpaceId);
        }

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(id);

        return await ResponseAsync(project, ct);
    }

    public async Task<ProjectResponse> ChangeStatusAsync(Guid id, ChangeProjectStatusRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);
        var oldStatus = project.Status;

        project.ChangeStatus(request.Status);
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(id);

        logger.LogInformation(
            "ProjectStatusChanged {ProjectId} {OldStatus} {NewStatus} {UserId}", id, oldStatus, project.Status, currentUser.UserId);

        return await ResponseAsync(project, ct);
    }

    public async Task<ProjectResponse> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Archive();
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(id);

        logger.LogInformation("ProjectArchived {ProjectId} {UserId}", id, currentUser.UserId);

        return await ResponseAsync(project, ct);
    }

    public async Task<ProjectResponse> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Restore();
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(id);

        logger.LogInformation("ProjectRestored {ProjectId} {UserId}", id, currentUser.UserId);

        return await ResponseAsync(project, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Projects.Remove(await db.FindOwnedProjectAsync(currentUser.UserId, id, ct));
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(id);

        logger.LogInformation("ProjectDeleted {ProjectId} {UserId}", id, currentUser.UserId);
    }

    // Um projeto só entra em um espaço do próprio usuário. Espaço de outra pessoa responde 404,
    // sem revelar que existe.
    private async Task EnsureOwnSpaceAsync(Guid? spaceId, CancellationToken ct)
    {
        if (spaceId is not { } id)
            return;

        var userId = currentUser.UserId;

        if (!await db.Spaces.AnyAsync(s => s.Id == id && s.OwnerId == userId, ct))
            throw new NotFoundException(nameof(Space), id);
    }

    private async Task<ProjectResponse> ResponseAsync(Project project, CancellationToken ct)
    {
        var spaceName = project.SpaceId is { } spaceId
            ? await db.Spaces.Where(s => s.Id == spaceId).Select(s => s.Name).FirstOrDefaultAsync(ct)
            : null;

        return ProjectResponse.From(project, spaceName);
    }
}
