using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common;

// Regra única de acesso a projetos, usada por todos os services:
//   - dono ou membro: lê o projeto e trabalha nas tarefas;
//   - só o dono: altera/exclui o projeto e gerencia membros.
public static class ProjectAccess
{
    public static IQueryable<Project> AccessibleProjects(this ITaskFlowDbContext db, Guid userId) =>
        db.Projects.Where(p =>
            p.OwnerId == userId || db.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId));

    // Sem acesso responde 404: não revela que o projeto existe.
    public static async Task<Project> FindAccessibleProjectAsync(
        this ITaskFlowDbContext db, Guid userId, Guid projectId, CancellationToken ct) =>
        await db.AccessibleProjects(userId).SingleOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException(nameof(Project), projectId);

    // Membro tentando ação de dono responde 403: ele já sabe que o projeto existe.
    public static async Task<Project> FindOwnedProjectAsync(
        this ITaskFlowDbContext db, Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await db.FindAccessibleProjectAsync(userId, projectId, ct);

        return project.OwnerId == userId
            ? project
            : throw new ForbiddenException("Only the project owner can do this.");
    }

    public static Task<bool> IsProjectPersonAsync(
        this ITaskFlowDbContext db, Guid projectId, Guid userId, CancellationToken ct) =>
        db.AccessibleProjects(userId).AnyAsync(p => p.Id == projectId, ct);
}
