using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

// Todo acesso é restrito aos projetos do usuário atual. Projeto de outro usuário responde 404,
// sem revelar que ele existe.
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
        var query = Owned().AsNoTracking().OrderByDescending(p => p.CreatedAt).AsQueryable();

        if (limit is { } take)
            query = query.Take(take);

        // Contagens calculadas no banco (subconsultas), sem carregar as tarefas.
        return await query
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.CreatedAt,
                db.Tasks.Count(t => t.ProjectId == p.Id),
                db.Tasks.Count(t => t.ProjectId == p.Id && t.Status == TaskItemStatus.Done)))
            .ToListAsync(ct);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        ProjectResponse.From(await FindAsync(id, ct));

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await FindAsync(id, ct);

        project.Update(request.Name, request.Description);
        await db.SaveChangesAsync(ct);

        return ProjectResponse.From(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Projects.Remove(await FindAsync(id, ct));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectDeleted {ProjectId} {UserId}", id, currentUser.UserId);
    }

    private IQueryable<Project> Owned() => db.Projects.Where(p => p.OwnerId == currentUser.UserId);

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) =>
        await Owned().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException(nameof(Project), id);
}
