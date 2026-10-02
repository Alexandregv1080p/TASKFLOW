using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

// Acesso conforme ProjectAccess: dono e membros leem; só o dono altera, arquiva e exclui.
public class ProjectService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<ProjectService> logger)
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project(request.Name, request.Description, currentUser.UserId);

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectCreated {ProjectId} {UserId}", project.Id, project.OwnerId);
        TaskFlowMetrics.ProjectsCreated.Add(1);

        return ProjectResponse.From(project);
    }

    // Diretório de projetos: escopo (aba), busca por nome e limite opcional.
    public async Task<IReadOnlyList<ProjectSummaryResponse>> ListAsync(
        ProjectScope scope = ProjectScope.All,
        string? search = null,
        int? limit = null,
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower + Contains vira LIKE sem diferenciar maiúsculas, sem depender de função específica do PostgreSQL.
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        if (limit is { } take)
            query = query.Take(take);

        // Contagens e nome do dono calculados no banco (subconsultas), sem carregar tarefas nem usuários.
        return await query
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
                db.Users.Where(u => u.Id == p.OwnerId).Select(u => u.Name).First(),
                p.Status,
                p.StatusUpdatedAt,
                p.ArchivedAt != null,
                p.CreatedAt,
                db.Tasks.Count(t => t.ProjectId == p.Id),
                db.Tasks.Count(t => t.ProjectId == p.Id && t.Status == TaskItemStatus.Done)))
            .ToListAsync(ct);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        ProjectResponse.From(await db.FindAccessibleProjectAsync(currentUser.UserId, id, ct));

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Update(request.Name, request.Description);
        await db.SaveChangesAsync(ct);

        return ProjectResponse.From(project);
    }

    public async Task<ProjectResponse> ChangeStatusAsync(Guid id, ChangeProjectStatusRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);
        var oldStatus = project.Status;

        project.ChangeStatus(request.Status);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ProjectStatusChanged {ProjectId} {OldStatus} {NewStatus} {UserId}", id, oldStatus, project.Status, currentUser.UserId);

        return ProjectResponse.From(project);
    }

    public async Task<ProjectResponse> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Archive();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectArchived {ProjectId} {UserId}", id, currentUser.UserId);

        return ProjectResponse.From(project);
    }

    public async Task<ProjectResponse> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, id, ct);

        project.Restore();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectRestored {ProjectId} {UserId}", id, currentUser.UserId);

        return ProjectResponse.From(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Projects.Remove(await db.FindOwnedProjectAsync(currentUser.UserId, id, ct));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectDeleted {ProjectId} {UserId}", id, currentUser.UserId);
    }
}
