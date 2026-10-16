using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");

        // Chave composta: o mesmo usuário não entra duas vezes no mesmo projeto.
        builder.HasKey(m => new { m.ProjectId, m.UserId });

        // Quem já era membro antes dos papéis continua membro (valor padrão da coluna).
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).HasDefaultValue(ProjectRole.Member);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Projetos de que sou membro".
        builder.HasIndex(m => m.UserId);
    }
}
