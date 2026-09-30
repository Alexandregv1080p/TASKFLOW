using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);

public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);

public record ChangeTaskStatusRequest(TaskItemStatus Status);

// UserId nulo remove o responsável.
public record AssignTaskRequest(Guid? UserId);

public record TaskResponse(
    Guid Id,
    Guid ProjectId,
    int Number,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? DueDate,
    Guid? AssignedToId,
    Guid? CreatedById,
    DateTime CreatedAt)
{
    public static TaskResponse From(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Number, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
            t.AssignedToId, t.CreatedById, t.CreatedAt);
}
