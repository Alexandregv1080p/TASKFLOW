using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Name).HasMaxLength(Goal.NameMaxLength).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(Goal.DescriptionMaxLength);
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(g => g.TargetDate).HasColumnType("date");

        // Metas são pessoais: somem junto com o usuário.
        builder.HasOne<User>().WithMany().HasForeignKey(g => g.OwnerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.OwnerId);
    }
}

public class GoalProjectConfiguration : IEntityTypeConfiguration<GoalProject>
{
    public void Configure(EntityTypeBuilder<GoalProject> builder)
    {
        builder.ToTable("GoalProjects");
        builder.HasKey(l => new { l.GoalId, l.ProjectId });

        // Excluir a meta ou o projeto desfaz a ligação, sem apagar o outro lado.
        builder.HasOne<Goal>().WithMany().HasForeignKey(l => l.GoalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.ProjectId);
    }
}
