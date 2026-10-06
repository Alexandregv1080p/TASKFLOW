using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class BoardHubTests(DatabaseFixture db)
{
    // Conexão com o hub da API em memória, autenticada com o mesmo token do cliente HTTP.
    private async Task<HubConnection> ConnectAsync(HttpClient client)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/board", o =>
            {
                o.HttpMessageHandlerFactory = _ => db.Api.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling; // o TestServer não abre WebSocket por este handler
                o.AccessTokenProvider = () => Task.FromResult(client.DefaultRequestHeaders.Authorization?.Parameter);
            })
            .Build();

        await connection.StartAsync();

        return connection;
    }

    [Fact]
    public async Task Quem_esta_no_quadro_e_avisado_quando_uma_tarefa_muda()
    {
        var owner = await db.CreateAuthenticatedClientAsync();
        var project = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Ao vivo" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString()!;

        await using var connection = await ConnectAsync(owner);
        var received = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("BoardChanged", change => received.TrySetResult(change));
        await connection.InvokeAsync("JoinProject", Guid.Parse(projectId));

        var task = await (await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "Nova", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var change = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(projectId, change.GetProperty("projectId").GetString());
        Assert.Equal(task.GetProperty("id").GetString(), change.GetProperty("taskId").GetString());
    }

    [Fact]
    public async Task Quem_nao_esta_no_projeto_nao_entra_no_quadro()
    {
        var owner = await db.CreateAuthenticatedClientAsync();
        var project = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Privado" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetGuid();

        await using var stranger = await ConnectAsync(await db.CreateAuthenticatedClientAsync("Intruso"));
        var received = false;
        stranger.On<JsonElement>("BoardChanged", _ => received = true);

        await Assert.ThrowsAsync<HubException>(() => stranger.InvokeAsync("JoinProject", projectId));

        // Mesmo conectado ao hub, sem entrar no grupo não recebe nada do projeto.
        await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "Secreta", priority = "Low" });
        await stranger.InvokeAsync("JoinProject", projectId).ContinueWith(_ => { }); // ida e volta: dá tempo de um aviso chegar
        Assert.False(received);
    }

    [Fact]
    public async Task Hub_exige_autenticacao()
    {
        var anonymous = db.Api.CreateClient();

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => ConnectAsync(anonymous));
    }
}
