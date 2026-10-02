using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Goals;

// ProjectIds: projetos que contribuem para a meta (podem ser nenhum).
public record GoalRequest(
    string Name,
    string? Description,
    DateTime? TargetDate,
    ProjectStatus Status,
    IReadOnlyList<Guid>? ProjectIds);

public record GoalProjectResponse(Guid Id, string Name, int TaskCount, int DoneCount);

// TaskCount/DoneCount somam as tarefas dos projetos ligados: é o progresso da meta.
public record GoalResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime? TargetDate,
    ProjectStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<GoalProjectResponse> Projects,
    int TaskCount,
    int DoneCount);

// Metas são pessoais: cada usuário só vê e altera as suas. Meta de outra pessoa responde 404.
public class GoalService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<GoalService> logger)
{
    public async Task<IReadOnlyList<GoalResponse>> ListAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var goals = await db.Goals.AsNoTracking()
            .Where(g => g.OwnerId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        var projects = await ProjectsOfAsync(goals.Select(g => g.Id).ToList(), ct);

        return goals.Select(g => ToResponse(g, projects[g.Id].ToList())).ToList();
    }

    public async Task<GoalResponse> CreateAsync(GoalRequest request, CancellationToken ct = default)
    {
        var projectIds = await ValidProjectIdsAsync(request.ProjectIds, ct);
        var goal = new Goal(currentUser.UserId, request.Name, request.Description, request.TargetDate, request.Status);

        db.Goals.Add(goal);
        db.GoalProjects.AddRange(projectIds.Select(id => new GoalProject(goal.Id, id)));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("GoalCreated {GoalId} {UserId}", goal.Id, currentUser.UserId);

        return await ResponseAsync(goal, ct);
    }

    public async Task<GoalResponse> UpdateAsync(Guid id, GoalRequest request, CancellationToken ct = default)
    {
        var goal = await FindAsync(id, ct);
        var projectIds = await ValidProjectIdsAsync(request.ProjectIds, ct);

        goal.Update(request.Name, request.Description, request.TargetDate, request.Status);

        // Troca o conjunto de projetos ligados pelo que veio na requisição.
        var links = await db.GoalProjects.Where(l => l.GoalId == goal.Id).ToListAsync(ct);
        db.GoalProjects.RemoveRange(links.Where(l => !projectIds.Contains(l.ProjectId)));
        db.GoalProjects.AddRange(projectIds
            .Where(pid => links.All(l => l.ProjectId != pid))
            .Select(pid => new GoalProject(goal.Id, pid)));

        await db.SaveChangesAsync(ct);

        return await ResponseAsync(goal, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Goals.Remove(await FindAsync(id, ct));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("GoalDeleted {GoalId} {UserId}", id, currentUser.UserId);
    }

    private async Task<Goal> FindAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        return await db.Goals.SingleOrDefaultAsync(g => g.Id == id && g.OwnerId == userId, ct)
            ?? throw new NotFoundException(nameof(Goal), id);
    }

    // Só dá para ligar projetos a que o usuário tem acesso; um id inválido responde 404, sem revelar o que existe.
    private async Task<List<Guid>> ValidProjectIdsAsync(IReadOnlyList<Guid>? requested, CancellationToken ct)
    {
        var ids = (requested ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return ids;

        var accessible = await db.AccessibleProjects(currentUser.UserId)
            .Where(p => ids.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var missing = ids.Except(accessible).ToList();

        return missing.Count == 0 ? ids : throw new NotFoundException(nameof(Project), missing[0]);
    }

    private async Task<GoalResponse> ResponseAsync(Goal goal, CancellationToken ct) =>
        ToResponse(goal, (await ProjectsOfAsync([goal.Id], ct))[goal.Id].ToList());

    // Projetos ligados a cada meta, com as contagens de tarefas calculadas no banco.
    // Se o usuário perdeu o acesso a um projeto ligado, ele deixa de contar (e de aparecer) na meta.
    private async Task<ILookup<Guid, GoalProjectResponse>> ProjectsOfAsync(List<Guid> goalIds, CancellationToken ct)
    {
        var accessible = db.AccessibleProjects(currentUser.UserId);

        var rows = await db.GoalProjects
            .Where(l => goalIds.Contains(l.GoalId))
            .Join(accessible, l => l.ProjectId, p => p.Id, (l, p) => new
            {
                l.GoalId,
                p.Id,
                p.Name,
                TaskCount = db.Tasks.Count(t => t.ProjectId == p.Id),
                DoneCount = db.Tasks.Count(t => t.ProjectId == p.Id && t.Status == TaskItemStatus.Done)
            })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return rows.ToLookup(r => r.GoalId, r => new GoalProjectResponse(r.Id, r.Name, r.TaskCount, r.DoneCount));
    }

    private static GoalResponse ToResponse(Goal goal, List<GoalProjectResponse> projects) =>
        new(goal.Id, goal.Name, goal.Description, goal.TargetDate, goal.Status, goal.CreatedAt,
            projects, projects.Sum(p => p.TaskCount), projects.Sum(p => p.DoneCount));
}
