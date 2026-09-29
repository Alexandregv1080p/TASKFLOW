using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Project : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }

    public Project(string name, string? description, Guid ownerId)
    {
        OwnerId = Guard.NotEmpty(ownerId, nameof(OwnerId));
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
    }

    public void Update(string name, string? description)
    {
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Description = Guard.Optional(description, DescriptionMaxLength, nameof(Description));
    }
}
