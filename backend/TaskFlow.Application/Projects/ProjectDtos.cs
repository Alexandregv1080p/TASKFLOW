using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

// Abas do diretório de projetos.
public enum ProjectScope
{
    // Projetos ativos em que sou dono ou membro.
    All,

    // Projetos ativos que eu criei.
    Mine,

    // Projetos arquivados a que tenho acesso.
    Archived
}

// SpaceId nulo = projeto sem espaço. Só dá para usar um espaço do próprio usuário.
public record CreateProjectRequest(string Name, string? Description, Guid? SpaceId = null);

public record UpdateProjectRequest(string Name, string? Description, Guid? SpaceId = null);

public record ChangeProjectStatusRequest(ProjectStatus Status);

public record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    Guid? SpaceId,
    string? SpaceName,
    ProjectStatus Status,
    DateTime StatusUpdatedAt,
    bool Archived,
    DateTime CreatedAt,
    int TaskCount,
    int DoneCount);

public record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    Guid? SpaceId,
    string? SpaceName,
    ProjectStatus Status,
    DateTime StatusUpdatedAt,
    DateTime? ArchivedAt,
    DateTime CreatedAt)
{
    public static ProjectResponse From(Project p, string? spaceName) =>
        new(p.Id, p.Name, p.Description, p.OwnerId, p.SpaceId, spaceName, p.Status, p.StatusUpdatedAt, p.ArchivedAt, p.CreatedAt);
}
