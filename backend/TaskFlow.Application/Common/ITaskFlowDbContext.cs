using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common;

// O DbContext já é repository + unit of work; a Application depende só desta interface,
// sem conhecer o provedor (PostgreSQL) que a Infrastructure configura.
public interface ITaskFlowDbContext
{
    DbSet<User> Users { get; }

    DbSet<Space> Spaces { get; }

    DbSet<Project> Projects { get; }

    DbSet<ProjectMember> ProjectMembers { get; }

    DbSet<TaskItem> Tasks { get; }

    DbSet<TaskType> TaskTypes { get; }

    DbSet<TaskAttachment> TaskAttachments { get; }

    DbSet<TaskEvent> TaskEvents { get; }

    DbSet<TaskComment> TaskComments { get; }

    DbSet<TaskStar> TaskStars { get; }

    DbSet<TaskView> TaskViews { get; }

    DbSet<Goal> Goals { get; }

    DbSet<GoalProject> GoalProjects { get; }

    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Espaço de anexos do projeto (Project.AttachmentBytes), mudado por UPDATE condicional no banco: "soma só se couber"
    // é atômico, então dois envios ao mesmo tempo não passam juntos da cota. true = reservou; false = não cabe.
    Task<bool> ReserveAttachmentBytesAsync(Guid projectId, long bytes, long quota, CancellationToken ct);

    // Devolve espaço (anexo excluído, envio que falhou depois de reservar). Nunca fica negativo.
    Task ReleaseAttachmentBytesAsync(Guid projectId, long bytes);
}
