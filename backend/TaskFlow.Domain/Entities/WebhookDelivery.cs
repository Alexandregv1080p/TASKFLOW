using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Uma entrega de evento para um webhook. É também a fila (outbox): nasce Pending na mesma transação da alteração
// que a gerou, e o despachante tenta entregar até dar certo ou esgotar as tentativas.
public class WebhookDelivery : Entity
{
    public const int EventMaxLength = 40;
    public const int ErrorMaxLength = 300;

    // 1 envio + 5 novas tentativas, com espera dobrando: 30 s, 1, 2, 4 e 8 min (~15 min no total).
    public const int MaxAttempts = 6;

    public Guid WebhookId { get; private set; }
    public string Event { get; private set; }

    // O corpo exato que será enviado (e assinado). Fica pronto desde a criação: reenviar manda o mesmo conteúdo.
    public string Payload { get; private set; }

    public WebhookDeliveryStatus Status { get; private set; } = WebhookDeliveryStatus.Pending;
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public int? LastStatusCode { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public WebhookDelivery(Guid webhookId, string @event, string payload)
    {
        WebhookId = Guard.NotEmpty(webhookId, nameof(WebhookId));
        Event = Guard.Required(@event, EventMaxLength, nameof(Event));
        Payload = string.IsNullOrEmpty(payload) ? throw new DomainException("Payload is required.") : payload;
        NextAttemptAt = CreatedAt;
    }

    // Usado pelo EF Core ao materializar do banco.
    private WebhookDelivery()
    {
        Event = null!;
        Payload = null!;
    }

    public void Succeed(int statusCode, DateTime now)
    {
        Attempts++;
        Status = WebhookDeliveryStatus.Succeeded;
        LastStatusCode = statusCode;
        LastError = null;
        DeliveredAt = now;
    }

    // Falhou (erro de rede, timeout ou resposta fora de 2xx): agenda a próxima tentativa ou desiste.
    public void Fail(int? statusCode, string error, DateTime now)
    {
        Attempts++;
        LastStatusCode = statusCode;
        LastError = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;

        if (Attempts >= MaxAttempts)
            Status = WebhookDeliveryStatus.Failed;
        else
            NextAttemptAt = now + Backoff(Attempts);
    }

    // Reenvio manual (pelo dono): volta para a fila do zero, com o mesmo conteúdo.
    public void Redeliver(DateTime now)
    {
        Status = WebhookDeliveryStatus.Pending;
        Attempts = 0;
        NextAttemptAt = now;
        LastStatusCode = null;
        LastError = null;
        DeliveredAt = null;
    }

    public static TimeSpan Backoff(int attempts) => TimeSpan.FromSeconds(30 * Math.Pow(2, attempts - 1));
}
