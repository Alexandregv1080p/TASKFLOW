using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Webhooks;

public class WebhookOptions
{
    // Desligado, as entregas ficam na fila (útil nos testes, que chamam DispatchDueAsync na mão).
    public bool DispatcherEnabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int TimeoutSeconds { get; init; } = 10;
    public int BatchSize { get; init; } = 20;

    // Só em desenvolvimento/testes: permite entregar em localhost e redes internas.
    public bool AllowPrivateNetworks { get; init; }
}

// Despachante da fila de entregas (o outbox). Cada rodada "reserva" um lote de pendentes vencidas, envia e grava o
// resultado. A reserva é um UPDATE com FOR UPDATE SKIP LOCKED que empurra NextAttemptAt para depois do prazo:
// várias réplicas da API dividem a fila sem pegar a mesma entrega, e se o processo cair no meio do envio, a
// entrega volta sozinha para a fila quando o prazo acaba. Entrega pelo menos uma vez: quem recebe usa o
// X-TaskFlow-Delivery para ignorar repetidas.
public sealed class WebhookDispatcher : IDisposable
{
    public const string SignatureHeader = "X-TaskFlow-Signature";
    public const string EventHeader = "X-TaskFlow-Event";
    public const string DeliveryHeader = "X-TaskFlow-Delivery";

    private readonly IServiceScopeFactory scopes;
    private readonly WebhookOptions options;
    private readonly ILogger<WebhookDispatcher> logger;
    private readonly HttpClient http;

    public WebhookDispatcher(IServiceScopeFactory scopes, WebhookOptions options, ILogger<WebhookDispatcher> logger)
    {
        this.scopes = scopes;
        this.options = options;
        this.logger = logger;

        // Um HttpClient para a vida do processo (com rotação das conexões), sem redirecionamento nem proxy:
        // um 302 para um endereço interno não pode contornar a checagem de rede.
        http = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectAsync,
        })
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TaskFlow-Webhooks/1.0");
    }

    public WebhookOptions Options => options;

    // Uma rodada: devolve quantas entregas tentou (0 = fila vazia por agora).
    public async Task<int> DispatchDueAsync(CancellationToken ct = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

        var now = DateTime.UtcNow;
        // O prazo da reserva cobre o lote inteiro no pior caso (todas dando timeout).
        var leaseUntil = now.AddSeconds(options.TimeoutSeconds * (options.BatchSize + 1));
        var ids = await db.Database.SqlQuery<Guid>($"""
            UPDATE "WebhookDeliveries" SET "NextAttemptAt" = {leaseUntil}
            WHERE "Id" IN (
                SELECT "Id" FROM "WebhookDeliveries"
                WHERE "Status" = 'Pending' AND "NextAttemptAt" <= {now}
                ORDER BY "NextAttemptAt"
                LIMIT {options.BatchSize}
                FOR UPDATE SKIP LOCKED)
            RETURNING "Id" AS "Value"
            """).ToListAsync(ct);
        if (ids.Count == 0)
            return 0;

        // Na ordem em que aconteceram (melhor esforço: uma entrega que falha não segura as seguintes).
        var deliveries = await db.WebhookDeliveries.Where(d => ids.Contains(d.Id)).OrderBy(d => d.CreatedAt).ToListAsync(ct);
        var webhookIds = deliveries.Select(d => d.WebhookId).Distinct().ToList();
        var webhooks = await db.Webhooks.AsNoTracking().Where(w => webhookIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, ct);

        // ponytail: envio em sequência; com muito volume, enviar o lote em paralelo (limitado por destino).
        foreach (var delivery in deliveries)
        {
            await SendAsync(webhooks[delivery.WebhookId], delivery, ct);
            await db.SaveChangesAsync(ct);
        }

        return deliveries.Count;
    }

    private async Task SendAsync(Webhook webhook, WebhookDelivery delivery, CancellationToken ct)
    {
        if (!webhook.Active && delivery.Event != "ping")
        {
            delivery.Fail(null, "Webhook is disabled.", DateTime.UtcNow);
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(EventHeader, delivery.Event);
        request.Headers.Add(DeliveryHeader, delivery.Id.ToString());
        request.Headers.Add(SignatureHeader, Sign(webhook.Secret, timestamp, delivery.Payload));

        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var code = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
                delivery.Succeed(code, DateTime.UtcNow);
            else
                delivery.Fail(code, $"HTTP {code} {response.ReasonPhrase}".Trim(), DateTime.UtcNow);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            delivery.Fail(null, e is TaskCanceledException ? $"Timed out after {options.TimeoutSeconds} s." : e.Message, DateTime.UtcNow);
        }

        logger.Log(delivery.Status == WebhookDeliveryStatus.Succeeded ? LogLevel.Information : LogLevel.Warning,
            "WebhookDelivery {DeliveryId} {WebhookId} {Event} {Status} {Attempt} {StatusCode} {ElapsedMs} {Error}",
            delivery.Id, webhook.Id, delivery.Event, delivery.Status, delivery.Attempts, delivery.LastStatusCode,
            watch.ElapsedMilliseconds, delivery.LastError);
    }

    // Formato no estilo Stripe: "t=<unix>,v1=<hex>", com o HMAC-SHA256 de "<t>.<corpo>". O tempo entra na assinatura
    // para quem recebe recusar reenvios antigos (replay) além de conferir o conteúdo.
    public static string Sign(string secret, long timestamp, string payload)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));

        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    // SSRF: a URL é de quem cadastrou, mas a requisição sai de dentro da nossa rede. A checagem é feita no IP
    // resolvido na hora de conectar (não no cadastro), então um nome que passa a apontar para 127.0.0.1 ou para o
    // endereço de metadados da nuvem (169.254.169.254) também é barrado.
    private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, ct);
        var allowed = options.AllowPrivateNetworks ? addresses : addresses.Where(a => !IsPrivate(a)).ToArray();
        if (allowed.Length == 0)
            throw new HttpRequestException($"Blocked: {host} resolves to a private or internal network address.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    // Loopback, redes privadas, link-local (inclui metadados da nuvem), CGNAT, "este host", multicast e as
    // equivalentes em IPv6. IPv6 que embute IPv4 (::ffff:10.0.0.1) é checado pelo IPv4.
    public static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var first = address.GetAddressBytes()[0];

            return address.Equals(IPAddress.IPv6None) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast || (first & 0xFE) == 0xFC;
        }

        var b = address.GetAddressBytes();

        return b[0] switch
        {
            0 or 10 or 127 => true,
            100 => b[1] >= 64 && b[1] <= 127,
            169 => b[1] == 254,
            172 => b[1] >= 16 && b[1] <= 31,
            192 => b[1] == 168 || (b[1] == 0 && b[2] is 0 or 2),
            198 => b[1] is 18 or 19,
            >= 224 => true,
            _ => false,
        };
    }

    public void Dispose() => http.Dispose();
}
