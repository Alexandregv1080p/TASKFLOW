using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Configurations;

public class TaskStarConfiguration : IEntityTypeConfiguration<TaskStar>
{
    public void Configure(EntityTypeBuilder<TaskStar> builder)
    {
        builder.ToTable("TaskStars");

        // Chave composta: uma estrela por usuário e tarefa.
        builder.HasKey(s => new { s.UserId, s.TaskId });

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(s => s.TaskId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.TaskId);
    }
}

public class TaskViewConfiguration : IEntityTypeConfiguration<TaskView>
{
    public void Configure(EntityTypeBuilder<TaskView> builder)
    {
        builder.ToTable("TaskViews");
        builder.HasKey(v => new { v.UserId, v.TaskId });

        builder.HasOne<User>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(v => v.TaskId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.TaskId);
    }
}
