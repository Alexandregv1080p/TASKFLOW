using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

// Acesso conforme ProjectAccess: dono e membros leem; só o dono altera e exclui.
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

    public async Task<IReadOnlyList<ProjectSummaryResponse>> ListAsync(int? limit = null, CancellationToken ct = default)
    {
        var query = db.AccessibleProjects(currentUser.UserId).AsNoTracking().OrderByDescending(p => p.CreatedAt).AsQueryable();

        if (limit is { } take)
            query = query.Take(take);

        // Contagens calculadas no banco (subconsultas), sem carregar as tarefas.
        return await query
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
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

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Projects.Remove(await db.FindOwnedProjectAsync(currentUser.UserId, id, ct));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectDeleted {ProjectId} {UserId}", id, currentUser.UserId);
    }
}
