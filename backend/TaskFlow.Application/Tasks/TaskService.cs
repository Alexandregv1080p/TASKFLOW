using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks;

public class TaskService(ITaskFlowDbContext db)
{
    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);

        var task = new TaskItem(projectId, request.Title, request.Description, request.Priority, request.DueDate);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        return TaskResponse.From(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);

        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.CreatedAt)
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

        task.ChangeStatus(request.Status);
        await db.SaveChangesAsync(ct);

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

    private async Task<TaskItem> FindAsync(Guid id, CancellationToken ct) =>
        await db.Tasks.FindAsync([id], ct) ?? throw new NotFoundException(nameof(TaskItem), id);

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new NotFoundException(nameof(Project), projectId);
    }
}
