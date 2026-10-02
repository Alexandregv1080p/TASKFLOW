using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Dashboard;

public record StatusCounts(int Todo, int InProgress, int Done);

public record PriorityCounts(int Low, int Medium, int High);

public record DashboardResponse(
    int TotalProjects,
    int TotalTasks,
    StatusCounts TasksByStatus,
    PriorityCounts TasksByPriority,
    IReadOnlyList<ProjectSummaryResponse> RecentProjects);

public class DashboardService(ITaskFlowDbContext db, ProjectService projects, ICurrentUser currentUser)
{
    private const int RecentProjectsLimit = 5;

    public async Task<DashboardResponse> GetAsync(CancellationToken ct = default)
    {
        // Projetos ativos, próprios e compartilhados comigo (arquivados ficam fora dos números).
        var accessible = db.AccessibleProjects(currentUser.UserId).Where(p => p.ArchivedAt == null);
        var tasks = db.Tasks.Where(t => accessible.Any(p => p.Id == t.ProjectId));

        var byStatus = await tasks
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var byPriority = await tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return new DashboardResponse(
            await accessible.CountAsync(ct),
            byStatus.Values.Sum(),
            new StatusCounts(
                byStatus.GetValueOrDefault(TaskItemStatus.Todo),
                byStatus.GetValueOrDefault(TaskItemStatus.InProgress),
                byStatus.GetValueOrDefault(TaskItemStatus.Done)),
            new PriorityCounts(
                byPriority.GetValueOrDefault(TaskPriority.Low),
                byPriority.GetValueOrDefault(TaskPriority.Medium),
                byPriority.GetValueOrDefault(TaskPriority.High)),
            await projects.ListAsync(limit: RecentProjectsLimit, ct: ct));
    }
}
