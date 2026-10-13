using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

// Aviso para uma pessoa sobre algo que outra fez em uma tarefa.
public class Notification : Entity
{
    public Guid UserId { get; private set; }

    public Guid ActorId { get; private set; }

    public Guid TaskId { get; private set; }

    public NotificationType Type { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public Notification(Guid userId, Guid actorId, Guid taskId, NotificationType type)
    {
        UserId = Guard.NotEmpty(userId, nameof(UserId));
        ActorId = Guard.NotEmpty(actorId, nameof(ActorId));
        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
        Type = type;
    }

    public void MarkRead() => ReadAt ??= DateTime.UtcNow;

    // Usado pelo EF Core ao materializar do banco.
    private Notification()
    {
    }
}
