using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public class Project : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }

    public ProjectStatus Status { get; private set; } = ProjectStatus.OnTrack;
    public DateTime StatusUpdatedAt { get; private set; } = DateTime.UtcNow;

    // Arquivado = somente leitura e fora das listagens do dia a dia. Nulo = ativo.
    public DateTime? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    // Espaço a que o projeto pertence. Nulo = projeto avulso, sem espaço.
    public Guid? SpaceId { get; private set; }

    public Project(string name, string? description, Guid ownerId, Guid? spaceId = null)
    {
        SpaceId = spaceId;
        OwnerId = Guard.NotEmpty(ownerId, nameof(OwnerId));
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
    }

    public void Update(string name, string? description)
    {
        EnsureActive();

        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
    }

    public void ChangeStatus(ProjectStatus status)
    {
        EnsureActive();

        if (!Enum.IsDefined(status))
            throw new DomainException($"Status '{status}' is invalid.");

        Status = status;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void MoveToSpace(Guid? spaceId)
    {
        EnsureActive();

        SpaceId = spaceId;
    }

    public void Archive() => ArchivedAt ??= DateTime.UtcNow;

    public void Restore() => ArchivedAt = null;

    // Projeto arquivado precisa ser restaurado antes de qualquer alteração.
    public void EnsureActive()
    {
        if (IsArchived)
            throw new DomainException("This project is archived. Restore it to make changes.");
    }
}
