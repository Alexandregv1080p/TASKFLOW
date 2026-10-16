using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks;

// Tipo, tarefa-pai (para criar uma subtarefa) e responsável são opcionais.
public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDate,
    Guid? TypeId = null,
    Guid? ParentId = null,
    Guid? AssignedToId = null);

public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);

public record ChangeTaskStatusRequest(TaskItemStatus Status);

// UserId nulo remove o responsável.
public record AssignTaskRequest(Guid? UserId);

// TypeId nulo deixa a tarefa sem tipo.
public record ChangeTaskTypeRequest(Guid? TypeId);

// ParentId nulo transforma a subtarefa de volta em tarefa comum.
public record ChangeTaskParentRequest(Guid? ParentId);

// OldValue/NewValue: nomes de enum (status, prioridade), data yyyy-MM-dd, id do usuário (responsável), id do tipo
// ou texto (título).
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
    DateTime CreatedAt,
    // Se o usuário atual marcou a tarefa com estrela.
    bool Starred,
    Guid? TypeId,
    Guid? ParentId)
{
    public static TaskResponse From(TaskItem t, bool starred) =>
        new(t.Id, t.ProjectId, t.Number, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
            t.AssignedToId, t.CreatedById, t.CreatedAt, starred, t.TypeId, t.ParentId);
}
