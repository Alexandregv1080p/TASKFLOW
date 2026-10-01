using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

// Usuário com quem o projeto foi compartilhado. O dono não aparece aqui: ele é Project.OwnerId.
public class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    public ProjectMember(Guid projectId, Guid userId)
    {
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        UserId = Guard.NotEmpty(userId, nameof(UserId));
    }
}
