using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Projects;

public record AddMemberRequest(string Email, ProjectRole Role = ProjectRole.Member);

public record ChangeMemberRoleRequest(ProjectRole Role);

// Sem e-mail: quem está no projeto vê só id, nome e papel dos colegas.
public record ProjectMemberResponse(Guid Id, string Name, bool IsOwner, ProjectRole Role);

public class ProjectMemberService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<ProjectMemberService> logger, ILiveNotifier? live = null)
{
    // Dono, membros e visualizadores. Só dono e membros podem ser responsáveis por tarefas.
    public async Task<IReadOnlyList<ProjectMemberResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        var people = await db.Users.AsNoTracking()
            .Where(u => u.Id == project.OwnerId
                || db.ProjectMembers.Any(m => m.ProjectId == project.Id && m.UserId == u.Id))
            .Select(u => new ProjectMemberResponse(
                u.Id,
                u.Name,
                u.Id == project.OwnerId,
                u.Id == project.OwnerId
                    ? ProjectRole.Owner
                    : db.ProjectMembers.Where(m => m.ProjectId == project.Id && m.UserId == u.Id).Select(m => m.Role).First()))
            .ToListAsync(ct);

        return people.OrderByDescending(p => p.IsOwner).ThenBy(p => p.Name).ToList();
    }

    public async Task<ProjectMemberResponse> AddAsync(Guid projectId, AddMemberRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, projectId, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new NotFoundException("No user is registered with this email.");

        if (await db.IsProjectPersonAsync(project.Id, user.Id, ct))
            throw new ConflictException("This user is already in the project.");

        var member = new ProjectMember(project.Id, user.Id, request.Role);
        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectMemberAdded {ProjectId} {MemberId} {UserId}", project.Id, user.Id, currentUser.UserId);
        await live.ChangedAsync(project.Id);

        return new ProjectMemberResponse(user.Id, user.Name, false, member.Role);
    }

    // Só o dono troca papéis. Quem vira visualizador deixa de ser responsável pelas tarefas do projeto.
    public async Task<ProjectMemberResponse> ChangeRoleAsync(
        Guid projectId, Guid userId, ChangeMemberRoleRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, projectId, ct);

        var member = await db.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(ProjectMember), userId);

        member.ChangeRole(request.Role);
        if (member.Role == ProjectRole.Viewer)
            await UnassignAllAsync(project.Id, userId, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectMemberRoleChanged {ProjectId} {MemberId} {Role} {UserId}", project.Id, userId, member.Role, currentUser.UserId);
        await live.ChangedAsync(project.Id);

        var name = await db.Users.Where(u => u.Id == userId).Select(u => u.Name).SingleAsync(ct);

        return new ProjectMemberResponse(userId, name, false, member.Role);
    }

    // O dono remove qualquer membro; um membro só remove a si mesmo (sair do projeto).
    public async Task RemoveAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        if (userId == project.OwnerId)
            throw new DomainException("The project owner cannot be removed.");

        if (currentUser.UserId != project.OwnerId && currentUser.UserId != userId)
            throw new ForbiddenException("Only the project owner can remove other members.");

        var member = await db.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(ProjectMember), userId);

        db.ProjectMembers.Remove(member);

        await UnassignAllAsync(project.Id, userId, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectMemberRemoved {ProjectId} {MemberId} {UserId}", project.Id, userId, currentUser.UserId);
        await live.ChangedAsync(project.Id);
    }

    // Quem sai do projeto ou vira visualizador não pode continuar responsável: as tarefas dele ficam sem
    // responsável, com registro no histórico, na mesma transação da mudança.
    private async Task UnassignAllAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var assigned = await db.Tasks.Where(t => t.ProjectId == projectId && t.AssignedToId == userId).ToListAsync(ct);

        foreach (var task in assigned)
        {
            task.Unassign();
            db.TaskEvents.Add(new TaskEvent(task.Id, currentUser.UserId, TaskEventType.AssigneeChanged, userId.ToString()));
        }
    }
}
