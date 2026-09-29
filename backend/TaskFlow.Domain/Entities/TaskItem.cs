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

    public TaskItem(Guid projectId, string title, string? description, TaskPriority priority, DateTime? dueDate)
    {
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        Title = Guard.Required(title, TitleMaxLength, nameof(Title));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
        Priority = ValidPriority(priority);
        DueDate = dueDate;
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
