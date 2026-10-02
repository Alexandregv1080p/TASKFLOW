using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Meta: um objetivo do usuário, opcionalmente ligado a projetos. O progresso vem das tarefas desses projetos.
public class Goal : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public Guid OwnerId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    // Data alvo, sem hora.
    public DateTime? TargetDate { get; private set; }

    // Mesma escala de situação usada nos projetos (No prazo, Em risco...).
    public ProjectStatus Status { get; private set; }

    public Goal(Guid ownerId, string name, string? description, DateTime? targetDate, ProjectStatus status)
    {
        OwnerId = Guard.NotEmpty(ownerId, nameof(OwnerId));
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
        TargetDate = targetDate?.Date;
        Status = ValidStatus(status);
    }

    public void Update(string name, string? description, DateTime? targetDate, ProjectStatus status)
    {
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
        TargetDate = targetDate?.Date;
        Status = ValidStatus(status);
    }

    private static ProjectStatus ValidStatus(ProjectStatus status) =>
        Enum.IsDefined(status) ? status : throw new DomainException($"Status '{status}' is invalid.");
}

// Ligação entre uma meta e um projeto que contribui para ela.
public class GoalProject
{
    public Guid GoalId { get; private set; }
    public Guid ProjectId { get; private set; }

    public GoalProject(Guid goalId, Guid projectId)
    {
        GoalId = Guard.NotEmpty(goalId, nameof(GoalId));
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
    }
}
