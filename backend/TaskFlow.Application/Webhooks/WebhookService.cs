using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Webhooks;

public record CreateWebhookRequest(string Url);

public record UpdateWebhookRequest(string Url, bool Active);

public record WebhookResponse(Guid Id, string Url, string Secret, bool Active, DateTime CreatedAt)
{
    public static WebhookResponse From(Webhook w) => new(w.Id, w.Url, w.Secret, w.Active, w.CreatedAt);
}

public record WebhookDeliveryResponse(
    Guid Id,
    string Event,
    WebhookDeliveryStatus Status,
    int Attempts,
    DateTime? NextAttemptAt,
    int? LastStatusCode,
    string? LastError,
    DateTime CreatedAt,
    DateTime? DeliveredAt,
    string Payload)
{
    public static WebhookDeliveryResponse From(WebhookDelivery d) => new(
        d.Id, d.Event, d.Status, d.Attempts,
        d.Status == WebhookDeliveryStatus.Pending ? d.NextAttemptAt : null,
        d.LastStatusCode, d.LastError, d.CreatedAt, d.DeliveredAt, d.Payload);
}

// Webhooks do projeto: só o dono vê e mexe (o segredo dá poder de forjar eventos). A entrega fica com o
// despachante; aqui é cadastro, histórico de entregas, teste e reenvio.
public class WebhookService(ITaskFlowDbContext db, ICurrentUser currentUser)
{
    // Histórico mostrado por webhook; as entregas antigas ficam no banco.
    private const int DeliveriesShown = 30;

    public async Task<IReadOnlyList<WebhookResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        await db.FindOwnedProjectAsync(currentUser.UserId, projectId, ct);

        var webhooks = await db.Webhooks.AsNoTracking()
            .Where(w => w.ProjectId == projectId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

        return webhooks.Select(WebhookResponse.From).ToList();
    }

    public async Task<WebhookResponse> CreateAsync(Guid projectId, CreateWebhookRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, projectId, ct);
        project.EnsureActive();

        if (await db.Webhooks.CountAsync(w => w.ProjectId == project.Id, ct) >= Webhook.MaxPerProject)
            throw new DomainException($"A project can have at most {Webhook.MaxPerProject} webhooks.");

        var webhook = new Webhook(project.Id, request.Url);
        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync(ct);

        return WebhookResponse.From(webhook);
    }

    public async Task<WebhookResponse> UpdateAsync(Guid id, UpdateWebhookRequest request, CancellationToken ct = default)
    {
        var (webhook, _) = await FindOwnedAsync(id, ct);

        webhook.Update(request.Url, request.Active);
        await db.SaveChangesAsync(ct);

        return WebhookResponse.From(webhook);
    }

    public async Task<WebhookResponse> RotateSecretAsync(Guid id, CancellationToken ct = default)
    {
        var (webhook, _) = await FindOwnedAsync(id, ct);

        webhook.RotateSecret();
        await db.SaveChangesAsync(ct);

        return WebhookResponse.From(webhook);
    }

    // As entregas vão junto (FK em cascata).
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var (webhook, _) = await FindOwnedAsync(id, ct);

        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync(ct);
    }

    // Evento "ping" só para este webhook: confere URL, rede e verificação da assinatura do outro lado.
    public async Task<WebhookDeliveryResponse> PingAsync(Guid id, CancellationToken ct = default)
    {
        var (webhook, project) = await FindOwnedAsync(id, ct);

        var delivery = new WebhookDelivery(webhook.Id, "ping", WebhookOutbox.Serialize(new
        {
            @event = "ping",
            occurredAt = DateTime.UtcNow,
            project = new { id = project.Id, name = project.Name, key = ProjectKey.For(project.Name) },
            webhook = new { id = webhook.Id, url = webhook.Url },
        }));
        db.WebhookDeliveries.Add(delivery);
        await db.SaveChangesAsync(ct);

        return WebhookDeliveryResponse.From(delivery);
    }

    public async Task<IReadOnlyList<WebhookDeliveryResponse>> DeliveriesAsync(Guid id, CancellationToken ct = default)
    {
        await FindOwnedAsync(id, ct);

        var deliveries = await db.WebhookDeliveries.AsNoTracking()
            .Where(d => d.WebhookId == id)
            .OrderByDescending(d => d.CreatedAt)
            .Take(DeliveriesShown)
            .ToListAsync(ct);

        return deliveries.Select(WebhookDeliveryResponse.From).ToList();
    }

    public async Task<WebhookDeliveryResponse> RedeliverAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var delivery = await db.WebhookDeliveries.SingleOrDefaultAsync(d => d.Id == deliveryId, ct)
            ?? throw new NotFoundException(nameof(WebhookDelivery), deliveryId);
        await FindOwnedAsync(delivery.WebhookId, ct);

        delivery.Redeliver(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        return WebhookDeliveryResponse.From(delivery);
    }

    // Webhook de projeto sem acesso: 404. Membro ou visualizador: 403 (a regra do ProjectAccess).
    private async Task<(Webhook Webhook, Project Project)> FindOwnedAsync(Guid id, CancellationToken ct)
    {
        var webhook = await db.Webhooks.SingleOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException(nameof(Webhook), id);

        var project = await db.FindOwnedProjectAsync(currentUser.UserId, webhook.ProjectId, ct);

        return (webhook, project);
    }
}
