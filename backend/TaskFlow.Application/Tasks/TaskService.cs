using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks;

// Tarefas só são acessíveis através de projetos do usuário atual.
public class TaskService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<TaskService> logger)
{
    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        await EnsureProjectAccessAsync(projectId, ct);

        // ponytail: próximo número via MAX+1; duas criações simultâneas no mesmo projeto colidem no índice único
        // (ProjectId, Number) e uma delas falha. Trocar por sequência por projeto se houver concorrência real.
        var number = (await db.Tasks.Where(t => t.ProjectId == projectId).MaxAsync(t => (int?)t.Number, ct) ?? 0) + 1;

        var task = new TaskItem(
            projectId, number, currentUser.UserId, request.Title, request.Description, request.Priority, request.DueDate);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("TaskCreated {TaskId} {ProjectId} {UserId}", task.Id, projectId, currentUser.UserId);
        TaskFlowMetrics.TasksCreated.Add(1);

        return TaskResponse.From(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await EnsureProjectAccessAsync(projectId, ct);

        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Number)
            .ToListAsync(ct);

        return tasks.Select(TaskResponse.From).ToList();
    }

    public async Task<TaskResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        TaskResponse.From(await FindAsync(id, ct));

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);

        task.Update(request.Title, request.Description, request.DueDate);
        task.ChangePriority(request.Priority);
        await db.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        db.Tasks.Remove(await FindAsync(id, ct));
        await db.SaveChangesAsync(ct);
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var oldStatus = task.Status;

        task.ChangeStatus(request.Status);
        await db.SaveChangesAsync(ct);

        if (oldStatus != task.Status)
        {
            logger.LogInformation(
                "TaskStatusChanged {TaskId} {OldStatus} {NewStatus} {UserId}", task.Id, oldStatus, task.Status, currentUser.UserId);

            if (task.Status == TaskItemStatus.Done)
                TaskFlowMetrics.TasksCompleted.Add(1);
        }

        return TaskResponse.From(task);
    }

    public async Task<TaskResponse> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);

        if (request.UserId is not { } userId)
        {
            task.Unassign();
        }
        else
        {
            if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
                throw new NotFoundException(nameof(User), userId);

            task.AssignTo(userId);
        }

        await db.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    private IQueryable<TaskItem> Accessible() =>
        db.Tasks.Where(t => db.Projects.Any(p => p.Id == t.ProjectId && p.OwnerId == currentUser.UserId));

    private async Task<TaskItem> FindAsync(Guid id, CancellationToken ct) =>
        await Accessible().SingleOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException(nameof(TaskItem), id);

    private async Task EnsureProjectAccessAsync(Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == currentUser.UserId, ct))
            throw new NotFoundException(nameof(Project), projectId);
    }
}
