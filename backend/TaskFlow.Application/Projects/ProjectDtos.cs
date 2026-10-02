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

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);

public record ChangeProjectStatusRequest(ProjectStatus Status);

public record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
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
    ProjectStatus Status,
    DateTime StatusUpdatedAt,
    DateTime? ArchivedAt,
    DateTime CreatedAt)
{
    public static ProjectResponse From(Project p) =>
        new(p.Id, p.Name, p.Description, p.OwnerId, p.Status, p.StatusUpdatedAt, p.ArchivedAt, p.CreatedAt);
}
