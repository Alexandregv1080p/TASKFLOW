using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Usuário com quem o projeto foi compartilhado. O dono não aparece aqui: ele é Project.OwnerId.
public class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;
    public ProjectRole Role { get; private set; } = ProjectRole.Member;

    public ProjectMember(Guid projectId, Guid userId, ProjectRole role = ProjectRole.Member)
    {
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        UserId = Guard.NotEmpty(userId, nameof(UserId));
        Role = ValidRole(role);
    }

    public void ChangeRole(ProjectRole role) => Role = ValidRole(role);

    // Dono só existe um, e é o do projeto: compartilhar dá papel de membro ou de visualizador.
    private static ProjectRole ValidRole(ProjectRole role) =>
        role is ProjectRole.Member or ProjectRole.Viewer
            ? role
            : throw new DomainException("A shared person can only be a Member or a Viewer.");
}
