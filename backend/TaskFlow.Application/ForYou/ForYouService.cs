using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.ForYou;

// Abas da página "Para você".
public enum ForYouFilter
{
    // Tarefas abertas atribuídas a mim + abertas em que trabalhei.
    Recommended,

    // Tarefas abertas atribuídas a mim.
    Assigned,

    // Tarefas que marquei com estrela.
    Starred,

    // Tarefas que alterei ou comentei.
    Worked,

    // Tarefas que abri.
    Viewed
}

// Date é o momento que colocou a tarefa na aba (última atividade, quando marquei, quando abri...);
// o frontend agrupa por ele (Hoje, Ontem, Nesta semana...).
public record ForYouItem(
    Guid TaskId,
    Guid ProjectId,
    string ProjectName,
    int Number,
    string Title,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssignedToId,
    string? AssigneeName,
    bool Starred,
    DateTime Date);

public record ForYouResponse(
    IReadOnlyList<ProjectSummaryResponse> RecommendedProjects,
    int AssignedCount,
    IReadOnlyList<ForYouItem> Items);

public class ForYouService(ITaskFlowDbContext db, ICurrentUser currentUser, ProjectService projects)
{
    private const int RecommendedProjectsLimit = 3;

    // ponytail: sem paginação; as 50 mais recentes de cada aba bastam para uma página inicial.
    // Acrescentar cursor por data se alguém precisar ir além.
    private const int ItemsLimit = 50;

    public async Task<ForYouResponse> GetAsync(ForYouFilter filter, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;

        // Só projetos ativos: arquivados não aparecem na página inicial.
        var activeProjects = db.AccessibleProjects(userId).Where(p => p.ArchivedAt == null);
        var tasks = db.Tasks.Where(t => activeProjects.Any(p => p.Id == t.ProjectId));

        var dates = filter switch
        {
            ForYouFilter.Assigned => await AssignedAsync(tasks, userId, ct),
            ForYouFilter.Starred => await db.TaskStars
                .Where(s => s.UserId == userId && tasks.Any(t => t.Id == s.TaskId))
                .ToDictionaryAsync(s => s.TaskId, s => s.CreatedAt, ct),
            ForYouFilter.Viewed => await db.TaskViews
                .Where(v => v.UserId == userId && tasks.Any(t => t.Id == v.TaskId))
                .ToDictionaryAsync(v => v.TaskId, v => v.ViewedAt, ct),
            ForYouFilter.Worked => await WorkedAsync(tasks, userId, ct),
            _ => Merge(await AssignedAsync(tasks, userId, ct), await WorkedAsync(tasks, userId, ct))
        };

        var ids = dates.Keys.ToList();

        var rows = await tasks.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            // "Recomendado" é o que ainda pede ação: tarefas concluídas ficam de fora.
            .Where(t => filter != ForYouFilter.Recommended || t.Status != TaskItemStatus.Done)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                ProjectName = db.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).First(),
                t.Number,
                t.Title,
                t.Status,
                t.Priority,
                t.AssignedToId,
                AssigneeName = db.Users.Where(u => u.Id == t.AssignedToId).Select(u => u.Name).FirstOrDefault(),
                Starred = db.TaskStars.Any(s => s.UserId == userId && s.TaskId == t.Id)
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ForYouItem(
                r.Id, r.ProjectId, r.ProjectName, r.Number, r.Title, r.Status, r.Priority,
                r.AssignedToId, r.AssigneeName, r.Starred, dates[r.Id]))
            .OrderByDescending(i => i.Date)
            .Take(ItemsLimit)
            .ToList();

        return new ForYouResponse(
            await projects.ListAsync(limit: RecommendedProjectsLimit, ct: ct),
            await tasks.CountAsync(t => t.AssignedToId == userId && t.Status != TaskItemStatus.Done, ct),
            items);
    }

    // Abertas e atribuídas a mim; a data é a última movimentação da tarefa (de qualquer pessoa).
    private async Task<Dictionary<Guid, DateTime>> AssignedAsync(
        IQueryable<Domain.Entities.TaskItem> tasks, Guid userId, CancellationToken ct) =>
        await tasks
            .Where(t => t.AssignedToId == userId && t.Status != TaskItemStatus.Done)
            .Select(t => new
            {
                t.Id,
                Date = db.TaskEvents.Where(e => e.TaskId == t.Id).Max(e => (DateTime?)e.CreatedAt) ?? t.CreatedAt
            })
            .ToDictionaryAsync(x => x.Id, x => x.Date, ct);

    // Tarefas em que eu mexi (alteração registrada no histórico ou comentário); a data é a minha última ação.
    private async Task<Dictionary<Guid, DateTime>> WorkedAsync(
        IQueryable<Domain.Entities.TaskItem> tasks, Guid userId, CancellationToken ct)
    {
        var events = await db.TaskEvents
            .Where(e => e.UserId == userId && tasks.Any(t => t.Id == e.TaskId))
            .GroupBy(e => e.TaskId)
            .Select(g => new { Id = g.Key, Date = g.Max(e => e.CreatedAt) })
            .ToDictionaryAsync(x => x.Id, x => x.Date, ct);

        var comments = await db.TaskComments
            .Where(c => c.AuthorId == userId && tasks.Any(t => t.Id == c.TaskId))
            .GroupBy(c => c.TaskId)
            .Select(g => new { Id = g.Key, Date = g.Max(c => c.CreatedAt) })
            .ToDictionaryAsync(x => x.Id, x => x.Date, ct);

        return Merge(events, comments);
    }

    // União das duas listas, ficando com a data mais recente de cada tarefa.
    private static Dictionary<Guid, DateTime> Merge(Dictionary<Guid, DateTime> a, Dictionary<Guid, DateTime> b)
    {
        foreach (var (id, date) in b)
            a[id] = a.TryGetValue(id, out var existing) && existing > date ? existing : date;

        return a;
    }
}
