using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.Webhooks;
using TaskFlow.IntegrationTests.Api;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Webhooks;

[Collection(DatabaseCollection.Name)]
public class WebhookTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _owner = null!;
    private string _projectId = null!;
    private readonly Receiver _receiver = new();

    public async Task InitializeAsync()
    {
        await _receiver.StartAsync();
        _owner = await db.CreateAuthenticatedClientAsync("Dona Webhook");
        var project = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Integra" })).Content.ReadFromJsonAsync<JsonElement>();
        _projectId = project.GetProperty("id").GetString()!;
    }

    public async Task DisposeAsync() => await _receiver.DisposeAsync();

    private WebhookDispatcher Dispatcher(WebApplicationFactory<Program>? api = null) =>
        (api ?? db.Api).Services.GetRequiredService<WebhookDispatcher>();

    private async Task<JsonElement> CreateWebhookAsync(string? url = null)
    {
        var response = await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/webhooks", new { url = url ?? _receiver.Url });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement[]> DeliveriesAsync(string webhookId) =>
        (await _owner.GetFromJsonAsync<JsonElement>($"/api/webhooks/{webhookId}/deliveries")).EnumerateArray().ToArray();

    [Fact]
    public async Task Alteracoes_na_tarefa_viram_eventos_assinados_e_entregues()
    {
        var webhook = await CreateWebhookAsync();
        var secret = webhook.GetProperty("secret").GetString()!;

        var task = await (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Checkout", priority = "High" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var taskId = task.GetProperty("id").GetString();
        (await _owner.PatchAsJsonAsync($"/api/tasks/{taskId}/status", new { status = "InProgress" })).EnsureSuccessStatusCode();
        (await _owner.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new { body = "Começando" })).EnsureSuccessStatusCode();
        (await _owner.DeleteAsync($"/api/tasks/{taskId}")).EnsureSuccessStatusCode();

        // A fila foi gravada junto com as alterações; nada foi enviado ainda.
        Assert.Empty(_receiver.Requests);
        await Dispatcher().DispatchDueAsync();

        var received = _receiver.Requests.ToArray();
        Assert.Equal(["task.created", "task.status_changed", "comment.created", "task.deleted"], received.Select(r => r.Event));

        foreach (var request in received)
        {
            // Quem recebe confere a assinatura com o segredo: "t=<unix>,v1=<hmac>" sobre "<t>.<corpo>".
            var timestamp = long.Parse(request.Signature.Split(',')[0][2..]);
            Assert.Equal(WebhookDispatcher.Sign(secret, timestamp, request.Body), request.Signature);
            Assert.True(Guid.TryParse(request.Delivery, out _));
        }

        var statusChanged = JsonDocument.Parse(received[1].Body).RootElement;
        Assert.Equal("INT-1", statusChanged.GetProperty("task").GetProperty("key").GetString());
        Assert.Equal("InProgress", statusChanged.GetProperty("task").GetProperty("status").GetString());
        Assert.Equal("status", statusChanged.GetProperty("change").GetProperty("field").GetString());
        Assert.Equal("Dona Webhook", statusChanged.GetProperty("actor").GetProperty("name").GetString());
        Assert.Equal("Começando", JsonDocument.Parse(received[2].Body).RootElement.GetProperty("change").GetProperty("body").GetString());

        var deliveries = await DeliveriesAsync(webhook.GetProperty("id").GetString()!);
        Assert.All(deliveries, d => Assert.Equal("Succeeded", d.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task Falha_agenda_nova_tentativa_e_o_dono_pode_reenviar()
    {
        var webhook = await CreateWebhookAsync();
        var webhookId = webhook.GetProperty("id").GetString()!;
        _receiver.StatusCode = 500;

        Assert.Equal(HttpStatusCode.Accepted, (await _owner.PostAsync($"/api/webhooks/{webhookId}/ping", null)).StatusCode);
        await Dispatcher().DispatchDueAsync();

        var failed = (await DeliveriesAsync(webhookId)).Single();
        Assert.Equal("Pending", failed.GetProperty("status").GetString());
        Assert.Equal(1, failed.GetProperty("attempts").GetInt32());
        Assert.Equal(500, failed.GetProperty("lastStatusCode").GetInt32());
        Assert.True(failed.GetProperty("nextAttemptAt").GetDateTime() > DateTime.UtcNow.AddSeconds(20));

        // Ainda não venceu: a rodada seguinte não tenta de novo.
        await Dispatcher().DispatchDueAsync();
        Assert.Single(_receiver.Requests);

        _receiver.StatusCode = 200;
        var redelivery = await _owner.PostAsync($"/api/webhook-deliveries/{failed.GetProperty("id").GetString()}/redeliver", null);
        Assert.Equal(HttpStatusCode.Accepted, redelivery.StatusCode);
        await Dispatcher().DispatchDueAsync();

        var delivered = (await DeliveriesAsync(webhookId)).Single();
        Assert.Equal("Succeeded", delivered.GetProperty("status").GetString());
        Assert.Equal(2, _receiver.Requests.Count);
        Assert.Equal(_receiver.Requests.First().Body, _receiver.Requests.Last().Body);
    }

    [Fact]
    public async Task Webhook_desativado_nao_gera_eventos()
    {
        var webhook = await CreateWebhookAsync();
        var webhookId = webhook.GetProperty("id").GetString()!;
        (await _owner.PutAsJsonAsync($"/api/webhooks/{webhookId}", new { url = _receiver.Url, active = false })).EnsureSuccessStatusCode();

        (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Silenciosa", priority = "Low" })).EnsureSuccessStatusCode();

        Assert.Empty(await DeliveriesAsync(webhookId));
    }

    [Fact]
    public async Task Endereco_interno_e_barrado_na_hora_do_envio()
    {
        // Sem a liberação de desenvolvimento, o receptor local (127.0.0.1) conta como rede interna.
        await using var api = db.Api.WithWebHostBuilder(b => b.UseSetting("Webhooks:AllowPrivateNetworks", "false"));
        var webhook = await CreateWebhookAsync();
        var webhookId = webhook.GetProperty("id").GetString()!;
        (await _owner.PostAsync($"/api/webhooks/{webhookId}/ping", null)).EnsureSuccessStatusCode();

        await Dispatcher(api).DispatchDueAsync();

        Assert.Empty(_receiver.Requests);
        var delivery = (await DeliveriesAsync(webhookId)).Single();
        Assert.Contains("private or internal network", delivery.GetProperty("lastError").GetString());
    }

    [Fact]
    public async Task Dois_despachantes_ao_mesmo_tempo_entregam_cada_evento_uma_vez()
    {
        await using var api = db.Api.WithWebHostBuilder(b => b.UseSetting("Webhooks:BatchSize", "2"));
        var webhookId = (await CreateWebhookAsync()).GetProperty("id").GetString()!;
        for (var i = 0; i < 10; i++)
            (await _owner.PostAsync($"/api/webhooks/{webhookId}/ping", null)).EnsureSuccessStatusCode();

        // Dois "processos" esvaziando a mesma fila em paralelo, em lotes pequenos.
        var dispatcher = Dispatcher(api);
        async Task DrainAsync()
        {
            while (await dispatcher.DispatchDueAsync() > 0)
            {
            }
        }

        await Task.WhenAll(DrainAsync(), DrainAsync(), DrainAsync());

        Assert.Equal(10, _receiver.Requests.Count);
        Assert.Equal(10, _receiver.Requests.Select(r => r.Delivery).Distinct().Count());
    }

    [Fact]
    public async Task So_o_dono_ve_e_mexe_nos_webhooks()
    {
        var webhookId = (await CreateWebhookAsync()).GetProperty("id").GetString()!;
        var member = await db.CreateAuthenticatedClientAsync("Membro");
        var stranger = await db.CreateAuthenticatedClientAsync("Estranho");
        var email = (await member.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("email").GetString();
        (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email })).EnsureSuccessStatusCode();

        await ApiTests.AssertErrorAsync(await member.GetAsync($"/api/projects/{_projectId}/webhooks"), HttpStatusCode.Forbidden);
        await ApiTests.AssertErrorAsync(await member.PostAsync($"/api/webhooks/{webhookId}/ping", null), HttpStatusCode.Forbidden);
        await ApiTests.AssertErrorAsync(await stranger.GetAsync($"/api/webhooks/{webhookId}/deliveries"), HttpStatusCode.NotFound);
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/webhooks", new { url = "ftp://example.com" }), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.20.0.5", true)]
    [InlineData("192.168.0.10", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("100.64.0.1", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("::1", true)]
    [InlineData("fd00::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("::ffff:10.0.0.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("2606:4700:4700::1111", false)]
    public void Classifica_enderecos_internos(string address, bool isPrivate) =>
        Assert.Equal(isPrivate, WebhookDispatcher.IsPrivate(IPAddress.Parse(address)));

    // Servidor HTTP de verdade (Kestrel em 127.0.0.1, porta livre) que guarda o que recebeu.
    private sealed class Receiver : IAsyncDisposable
    {
        public record Request(string Event, string Delivery, string Signature, string Body);

        private WebApplication? _app;

        public ConcurrentQueue<Request> Requests { get; } = new();
        public int StatusCode { get; set; } = 200;
        public string Url { get; private set; } = "";

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            _app = builder.Build();
            _app.MapPost("/hook", async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                Requests.Enqueue(new Request(
                    context.Request.Headers[WebhookDispatcher.EventHeader].ToString(),
                    context.Request.Headers[WebhookDispatcher.DeliveryHeader].ToString(),
                    context.Request.Headers[WebhookDispatcher.SignatureHeader].ToString(),
                    await reader.ReadToEndAsync()));

                return Results.StatusCode(StatusCode);
            });
            await _app.StartAsync();
            Url = _app.Urls.First() + "/hook";
        }

        public async ValueTask DisposeAsync()
        {
            if (_app is not null)
                await _app.DisposeAsync();
        }
    }
}
