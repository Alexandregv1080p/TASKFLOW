using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

public class ProjectService(ITaskFlowDbContext db)
{
    public async Task<ProjectResponse> CreateAsync(Guid ownerId, CreateProjectRequest request, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == ownerId, ct))
            throw new NotFoundException(nameof(User), ownerId);

        var project = new Project(request.Name, request.Description, ownerId);

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return ProjectResponse.From(project);
    }

    // ponytail: lista todos os projetos; filtro por dono/membro entra com a autorização (Fase 7).
    public async Task<IReadOnlyList<ProjectSummaryResponse>> ListAsync(int? limit = null, CancellationToken ct = default)
    {
        var query = db.Projects.AsNoTracking().OrderByDescending(p => p.CreatedAt).AsQueryable();

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
    }

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) =>
        await db.Projects.FindAsync([id], ct) ?? throw new NotFoundException(nameof(Project), id);
}
