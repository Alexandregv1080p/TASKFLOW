using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);

public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);

public record ChangeTaskStatusRequest(TaskItemStatus Status);

// UserId nulo remove o responsável.
public record AssignTaskRequest(Guid? UserId);

// OldValue/NewValue: nomes de enum (status, prioridade), data yyyy-MM-dd, id do usuário (responsável) ou texto (título).
public record TaskEventResponse(Guid Id, TaskEventType Type, string? OldValue, string? NewValue, Guid? UserId, DateTime CreatedAt);

public record AddCommentRequest(string Body);

public record TaskCommentResponse(Guid Id, Guid TaskId, Guid? AuthorId, string Body, DateTime CreatedAt)
{
    public static TaskCommentResponse From(TaskComment c) => new(c.Id, c.TaskId, c.AuthorId, c.Body, c.CreatedAt);
}

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
