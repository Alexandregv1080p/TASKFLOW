using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.ToTable("Webhooks");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Url).HasMaxLength(Webhook.UrlMaxLength);
        builder.Property(w => w.Secret).HasMaxLength(64);

        // Excluir o projeto leva os webhooks (e, por eles, as entregas).
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.ProjectId);
    }
}

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Event).HasMaxLength(WebhookDelivery.EventMaxLength);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.LastError).HasMaxLength(WebhookDelivery.ErrorMaxLength);

        builder.HasOne<Webhook>()
            .WithMany()
            .HasForeignKey(d => d.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);

        // A fila: o despachante busca "pendentes já vencidas" pela data; o índice parcial só tem as pendentes,
        // então não cresce com o histórico de entregas concluídas.
        builder.HasIndex(d => d.NextAttemptAt).HasFilter("\"Status\" = 'Pending'");
        builder.HasIndex(d => new { d.WebhookId, d.CreatedAt });
    }
}
