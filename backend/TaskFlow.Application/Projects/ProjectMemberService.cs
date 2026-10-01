using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Projects;

public record AddMemberRequest(string Email);

// Sem e-mail: quem está no projeto vê só id e nome dos colegas.
public record ProjectMemberResponse(Guid Id, string Name, bool IsOwner);

public class ProjectMemberService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<ProjectMemberService> logger)
{
    // Dono + membros: são as pessoas que podem ser responsáveis por tarefas do projeto.
    public async Task<IReadOnlyList<ProjectMemberResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        var people = await db.Users.AsNoTracking()
            .Where(u => u.Id == project.OwnerId
                || db.ProjectMembers.Any(m => m.ProjectId == project.Id && m.UserId == u.Id))
            .Select(u => new ProjectMemberResponse(u.Id, u.Name, u.Id == project.OwnerId))
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

        db.ProjectMembers.Add(new ProjectMember(project.Id, user.Id));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectMemberAdded {ProjectId} {MemberId} {UserId}", project.Id, user.Id, currentUser.UserId);

        return new ProjectMemberResponse(user.Id, user.Name, false);
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

        // Quem saiu não pode continuar responsável: as tarefas dele no projeto ficam sem responsável,
        // com registro no histórico, tudo na mesma transação.
        var assigned = await db.Tasks.Where(t => t.ProjectId == project.Id && t.AssignedToId == userId).ToListAsync(ct);

        foreach (var task in assigned)
        {
            task.Unassign();
            db.TaskEvents.Add(new TaskEvent(task.Id, currentUser.UserId, TaskEventType.AssigneeChanged, userId.ToString()));
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("ProjectMemberRemoved {ProjectId} {MemberId} {UserId}", project.Id, userId, currentUser.UserId);
    }
}
