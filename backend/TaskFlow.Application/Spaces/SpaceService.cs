using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Spaces;

// TemplateKey: modelo da galeria; vazio = espaço em branco.
public record CreateSpaceRequest(string Name, string? Description, string? TemplateKey = null);

public record UpdateSpaceRequest(string Name, string? Description);

// ProjectCount: projetos ativos do espaço que o usuário atual consegue ver.
public record SpaceSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    int ProjectCount,
    DateTime CreatedAt);

public record SpaceResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    DateTime CreatedAt,
    IReadOnlyList<ProjectSummaryResponse> Projects);

public record SpaceTemplateProjectResponse(string Name, string Description, int TaskCount);

public record SpaceTemplateResponse(
    string Key,
    string Name,
    string Description,
    string Category,
    bool Recommended,
    IReadOnlyList<SpaceTemplateProjectResponse> Projects);

// Regras de acesso:
//   - o dono do espaço edita, exclui e coloca projetos nele;
//   - quem participa de algum projeto do espaço enxerga o espaço, mas só com os projetos a que tem acesso;
//   - não há membros de espaço: o compartilhamento continua sendo por projeto.
public class SpaceService(
    ITaskFlowDbContext db,
    ICurrentUser currentUser,
    ProjectService projects,
    ILogger<SpaceService> logger)
{
    public IReadOnlyList<SpaceTemplateResponse> ListTemplates() =>
        SpaceTemplateCatalog.All
            .Select(t => new SpaceTemplateResponse(
                t.Key, t.Name, t.Description, t.Category, t.Recommended,
                t.Projects.Select(p => new SpaceTemplateProjectResponse(p.Name, p.Description, p.Tasks.Count)).ToList()))
            .ToList();

    public async Task<IReadOnlyList<SpaceSummaryResponse>> ListAsync(int? limit = null, CancellationToken ct = default)
    {
        var accessibleProjects = db.AccessibleProjects(currentUser.UserId);
        var query = Accessible().AsNoTracking().OrderByDescending(s => s.CreatedAt).AsQueryable();

        if (limit is { } take)
            query = query.Take(take);

        return await query
            .Select(s => new SpaceSummaryResponse(
                s.Id,
                s.Name,
                s.Description,
                s.OwnerId,
                db.Users.Where(u => u.Id == s.OwnerId).Select(u => u.Name).First(),
                accessibleProjects.Count(p => p.SpaceId == s.Id && p.ArchivedAt == null),
                s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<SpaceResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        await ResponseAsync(await FindAsync(id, ct), ct);

    // Cria o espaço e, se veio de um modelo, os projetos e as tarefas iniciais, tudo em uma transação.
    public async Task<SpaceResponse> CreateAsync(CreateSpaceRequest request, CancellationToken ct = default)
    {
        var template = SpaceTemplateCatalog.Find(request.TemplateKey)
            ?? throw new NotFoundException($"Space template '{request.TemplateKey}' was not found.");

        var userId = currentUser.UserId;
        var space = new Space(request.Name, request.Description, userId);
        db.Spaces.Add(space);

        foreach (var templateProject in template.Projects)
        {
            var project = new Project(templateProject.Name, templateProject.Description, userId, space.Id);
            db.Projects.Add(project);

            for (var i = 0; i < templateProject.Tasks.Count; i++)
            {
                var templateTask = templateProject.Tasks[i];
                var task = new TaskItem(project.Id, i + 1, userId, templateTask.Title, null, templateTask.Priority, null);

                db.Tasks.Add(task);
                db.TaskEvents.Add(new TaskEvent(task.Id, userId, TaskEventType.Created));
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("SpaceCreated {SpaceId} {Template} {UserId}", space.Id, template.Key, userId);
        TaskFlowMetrics.ProjectsCreated.Add(template.Projects.Count);
        TaskFlowMetrics.TasksCreated.Add(template.Projects.Sum(p => p.Tasks.Count));

        return await ResponseAsync(space, ct);
    }

    public async Task<SpaceResponse> UpdateAsync(Guid id, UpdateSpaceRequest request, CancellationToken ct = default)
    {
        var space = await FindOwnedAsync(id, ct);

        space.Update(request.Name, request.Description);
        await db.SaveChangesAsync(ct);

        return await ResponseAsync(space, ct);
    }

    // Os projetos do espaço não são apagados: ficam sem espaço (FK com SET NULL).
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Spaces.Remove(await FindOwnedAsync(id, ct));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("SpaceDeleted {SpaceId} {UserId}", id, currentUser.UserId);
    }

    // Espaços que eu criei ou que contêm algum projeto a que tenho acesso.
    private IQueryable<Space> Accessible()
    {
        var userId = currentUser.UserId;
        var accessibleProjects = db.AccessibleProjects(userId);

        return db.Spaces.Where(s => s.OwnerId == userId || accessibleProjects.Any(p => p.SpaceId == s.Id));
    }

    private async Task<Space> FindAsync(Guid id, CancellationToken ct) =>
        await Accessible().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException(nameof(Space), id);

    // Sem acesso: 404. Com acesso mas sem ser o dono: 403.
    private async Task<Space> FindOwnedAsync(Guid id, CancellationToken ct)
    {
        var space = await FindAsync(id, ct);

        return space.OwnerId == currentUser.UserId
            ? space
            : throw new ForbiddenException("Only the space owner can do this.");
    }

    private async Task<SpaceResponse> ResponseAsync(Space space, CancellationToken ct)
    {
        var ownerName = await db.Users.Where(u => u.Id == space.OwnerId).Select(u => u.Name).FirstAsync(ct);

        return new SpaceResponse(
            space.Id, space.Name, space.Description, space.OwnerId, ownerName, space.CreatedAt,
            await projects.ListAsync(spaceId: space.Id, ct: ct));
    }
}
