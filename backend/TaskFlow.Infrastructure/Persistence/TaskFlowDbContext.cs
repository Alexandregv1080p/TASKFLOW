using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : DbContext(options), ITaskFlowDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<TaskEvent> TaskEvents => Set<TaskEvent>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskStar> TaskStars => Set<TaskStar>();

    public DbSet<TaskView> TaskViews => Set<TaskView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
}
