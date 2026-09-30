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
        var userId = currentUser.UserId;
        var tasks = db.Tasks.Where(t => db.Projects.Any(p => p.Id == t.ProjectId && p.OwnerId == userId));

        var byStatus = await tasks
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var byPriority = await tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return new DashboardResponse(
            await db.Projects.CountAsync(p => p.OwnerId == userId, ct),
            byStatus.Values.Sum(),
            new StatusCounts(
                byStatus.GetValueOrDefault(TaskItemStatus.Todo),
                byStatus.GetValueOrDefault(TaskItemStatus.InProgress),
                byStatus.GetValueOrDefault(TaskItemStatus.Done)),
            new PriorityCounts(
                byPriority.GetValueOrDefault(TaskPriority.Low),
                byPriority.GetValueOrDefault(TaskPriority.Medium),
                byPriority.GetValueOrDefault(TaskPriority.High)),
            await projects.ListAsync(RecentProjectsLimit, ct));
    }
}
