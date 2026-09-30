using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Projects;

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);

public record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int TaskCount,
    int DoneCount);

public record ProjectResponse(Guid Id, string Name, string? Description, Guid OwnerId, DateTime CreatedAt)
{
    public static ProjectResponse From(Project p) => new(p.Id, p.Name, p.Description, p.OwnerId, p.CreatedAt);
}
