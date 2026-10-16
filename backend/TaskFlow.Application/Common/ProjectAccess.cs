using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Common;

// Regra única de acesso a projetos, usada por todos os services:
//   - dono, membro ou visualizador: lê o projeto;
//   - dono ou membro: trabalha nas tarefas (visualizador só lê);
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

    // Espaço: o dono, ou quem participa de algum projeto dentro dele.
    public static IQueryable<Space> AccessibleSpaces(this ITaskFlowDbContext db, Guid userId)
    {
        var projects = db.AccessibleProjects(userId);

        return db.Spaces.Where(s => s.OwnerId == userId || projects.Any(p => p.SpaceId == s.Id));
    }

    // Pode alterar tarefas (e ser responsável por elas): dono ou membro, não visualizador.
    public static Task<bool> CanEditProjectAsync(
        this ITaskFlowDbContext db, Guid projectId, Guid userId, CancellationToken ct) =>
        db.Projects.AnyAsync(p => p.Id == projectId
            && (p.OwnerId == userId
                || db.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId && m.Role == ProjectRole.Member)), ct);

    public static Task<bool> IsProjectPersonAsync(
        this ITaskFlowDbContext db, Guid projectId, Guid userId, CancellationToken ct) =>
        db.AccessibleProjects(userId).AnyAsync(p => p.Id == projectId, ct);
}
