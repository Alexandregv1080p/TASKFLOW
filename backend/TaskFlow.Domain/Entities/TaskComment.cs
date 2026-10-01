using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class TaskComment : Entity
{
    public const int BodyMaxLength = 2000;

    public Guid TaskId { get; private set; }

    // Nulo se o autor for removido depois; o comentário permanece.
    public Guid? AuthorId { get; private set; }

    public string Body { get; private set; }

    public TaskComment(Guid taskId, Guid authorId, string body)
    {
        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
        AuthorId = Guard.NotEmpty(authorId, nameof(AuthorId));
        Body = Guard.Required(body, BodyMaxLength, nameof(Body));
    }

    // Usado pelo EF Core ao materializar do banco.
    private TaskComment()
    {
        Body = null!;
    }
}
