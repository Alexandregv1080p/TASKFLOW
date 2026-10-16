using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public class TaskItem : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? AssignedToId { get; private set; }

    // Tipo da tarefa (do mesmo projeto); nulo = sem tipo.
    public Guid? TypeId { get; private set; }

    // Tarefa-pai, quando esta é uma subtarefa. Um nível só, como no Jira: subtarefa não tem subtarefas.
    public Guid? ParentId { get; private set; }

    // Sequencial dentro do projeto: forma a chave exibida (ex.: WEB-12).
    public int Number { get; private set; }

    // Nulo apenas em tarefas criadas antes da autenticação existir.
    public Guid? CreatedById { get; private set; }

    public TaskItem(
        Guid projectId,
        int number,
        Guid createdById,
        string title,
        string? description,
        TaskPriority priority,
        DateTime? dueDate)
    {
        if (number <= 0)
            throw new DomainException("Number must be positive.");

        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        Number = number;
        CreatedById = Guard.NotEmpty(createdById, nameof(CreatedById));
        Title = Guard.Required(title, TitleMaxLength, nameof(Title));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
        Priority = ValidPriority(priority);
        DueDate = dueDate;
    }

    // Usado pelo EF Core ao materializar do banco.
    private TaskItem()
    {
        Title = null!;
    }

    public void Update(string title, string? description, DateTime? dueDate)
    {
        Title = Guard.Required(title, TitleMaxLength, nameof(Title));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
        DueDate = dueDate;
    }

    public void ChangePriority(TaskPriority priority) => Priority = ValidPriority(priority);

    public void AssignTo(Guid userId) => AssignedToId = Guard.NotEmpty(userId, "UserId");

    public void Unassign() => AssignedToId = null;

    // A Application garante que o tipo é do mesmo projeto (precisa consultar o banco).
    public void ChangeType(Guid? typeId) => TypeId = typeId == Guid.Empty ? null : typeId;

    public void MakeSubtaskOf(TaskItem parent)
    {
        if (parent.Id == Id)
            throw new DomainException("A task cannot be its own parent.");

        if (parent.ProjectId != ProjectId)
            throw new DomainException("A subtask must be in the same project as its parent.");

        if (parent.ParentId is not null)
            throw new DomainException("A subtask cannot have subtasks.");

        ParentId = parent.Id;
    }

    // Fluxo do MVP: Todo <-> InProgress <-> Done, um passo por vez (sem pular etapas).
    public void ChangeStatus(TaskItemStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException($"Status '{status}' is invalid.");

        if (status == Status)
            return;

        if (Math.Abs(status - Status) != 1)
            throw new DomainException($"Cannot change status from {Status} to {status}.");

        Status = status;
    }

    private static TaskPriority ValidPriority(TaskPriority priority) =>
        Enum.IsDefined(priority) ? priority : throw new DomainException($"Priority '{priority}' is invalid.");
}
