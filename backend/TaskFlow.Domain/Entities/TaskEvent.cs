using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Registro imutável de uma alteração em uma tarefa (histórico/auditoria).
public class TaskEvent : Entity
{
    public const int ValueMaxLength = 200;

    public Guid TaskId { get; private set; }

    // Nulo se o usuário for removido depois; o evento permanece.
    public Guid? UserId { get; private set; }

    public TaskEventType Type { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    public TaskEvent(Guid taskId, Guid userId, TaskEventType type, string? oldValue = null, string? newValue = null)
    {
        if (!Enum.IsDefined(type))
            throw new DomainException($"Event type '{type}' is invalid.");

        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
        UserId = Guard.NotEmpty(userId, nameof(UserId));
        Type = type;
        OldValue = Guard.Optional(oldValue, ValueMaxLength, nameof(OldValue));
        NewValue = Guard.Optional(newValue, ValueMaxLength, nameof(NewValue));
    }

    // Usado pelo EF Core ao materializar do banco.
    private TaskEvent()
    {
    }
}
