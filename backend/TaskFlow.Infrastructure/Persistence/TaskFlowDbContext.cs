using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : DbContext(options), ITaskFlowDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Space> Spaces => Set<Space>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<TaskType> TaskTypes => Set<TaskType>();

    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    public DbSet<TaskEvent> TaskEvents => Set<TaskEvent>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskStar> TaskStars => Set<TaskStar>();

    public DbSet<TaskView> TaskViews => Set<TaskView>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<GoalProject> GoalProjects => Set<GoalProject>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public async Task<bool> ReserveAttachmentBytesAsync(Guid projectId, long bytes, long quota, CancellationToken ct) =>
        await Projects
            .Where(p => p.Id == projectId && p.AttachmentBytes + bytes <= quota)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AttachmentBytes, p => p.AttachmentBytes + bytes), ct) == 1;

    public Task ReleaseAttachmentBytesAsync(Guid projectId, long bytes) =>
        bytes <= 0
            ? Task.CompletedTask
            : Projects
                .Where(p => p.Id == projectId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.AttachmentBytes, p => p.AttachmentBytes > bytes ? p.AttachmentBytes - bytes : 0));

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
}
