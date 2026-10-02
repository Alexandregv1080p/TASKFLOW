using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

// Marcações pessoais de um usuário sobre uma tarefa. Alimentam a página "Para você".

// Tarefa marcada com estrela pelo usuário.
public class TaskStar
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public TaskStar(Guid userId, Guid taskId)
    {
        UserId = Guard.NotEmpty(userId, nameof(UserId));
        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
    }
}

// Última vez que o usuário abriu a tarefa (uma linha por usuário e tarefa).
public class TaskView
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTime ViewedAt { get; private set; } = DateTime.UtcNow;

    public TaskView(Guid userId, Guid taskId)
    {
        UserId = Guard.NotEmpty(userId, nameof(UserId));
        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
    }

    public void Touch() => ViewedAt = DateTime.UtcNow;
}
