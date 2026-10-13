using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class NotificationTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _owner = null!;
    private HttpClient _member = null!;
    private string _memberId = null!;
    private string _projectId = null!;

    // Dona e uma colega ("Bia Souza") no mesmo projeto.
    public async Task InitializeAsync()
    {
        _owner = await db.CreateAuthenticatedClientAsync("Dona");
        _member = await db.CreateAuthenticatedClientAsync("Bia Souza");

        var me = await _member.GetFromJsonAsync<JsonElement>("/api/auth/me");
        _memberId = me.GetProperty("id").GetString()!;

        var project = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Avisos" })).Content.ReadFromJsonAsync<JsonElement>();
        _projectId = project.GetProperty("id").GetString()!;

        var added = await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email = me.GetProperty("email").GetString() });
        added.EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> CreateTaskAsync(HttpClient client)
    {
        var task = await (await client.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Revisar", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        return task.GetProperty("id").GetString()!;
    }

    private static Task<JsonElement> ListAsync(HttpClient client) => client.GetFromJsonAsync<JsonElement>("/api/notifications");

    private static string[] Types(JsonElement list) =>
        list.GetProperty("items").EnumerateArray().Select(n => n.GetProperty("type").GetString()!).ToArray();

    [Fact]
    public async Task Atribuir_notifica_o_responsavel_mas_nao_quem_se_atribui()
    {
        var taskId = await CreateTaskAsync(_owner);

        await _owner.PatchAsJsonAsync($"/api/tasks/{taskId}/assignee", new { userId = _memberId });

        var list = await ListAsync(_member);
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());

        var item = list.GetProperty("items")[0];
        Assert.Equal("Assigned", item.GetProperty("type").GetString());
        Assert.Equal("Dona", item.GetProperty("actorName").GetString());
        Assert.Equal("Revisar", item.GetProperty("taskTitle").GetString());
        Assert.Equal(_projectId, item.GetProperty("projectId").GetString());
        Assert.False(item.GetProperty("read").GetBoolean());

        // Atribuir a si mesma não gera aviso.
        var own = await CreateTaskAsync(_member);
        await _member.PatchAsJsonAsync($"/api/tasks/{own}/assignee", new { userId = _memberId });
        Assert.Equal(1, (await ListAsync(_member)).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Comentario_notifica_mencionados_e_quem_acompanha_a_tarefa()
    {
        // Tarefa criada pela colega: ela acompanha os comentários.
        var taskId = await CreateTaskAsync(_member);

        await _owner.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new { body = "Comecei a olhar." });
        Assert.Equal(["Commented"], Types(await ListAsync(_member)));

        // Mencionada (sem diferenciar maiúsculas): recebe só o aviso de menção, não os dois.
        await _owner.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new { body = "@bia souza pode validar?" });
        Assert.Equal(["Mentioned", "Commented"], Types(await ListAsync(_member)));

        // Quem comenta não é avisada do próprio comentário, nem ao se mencionar.
        await _member.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new { body = "@Bia Souza anotado" });
        Assert.Equal(2, (await ListAsync(_member)).GetProperty("items").GetArrayLength());
        Assert.Equal(0, (await ListAsync(_owner)).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Marcar_como_lida_e_isolamento_entre_usuarios()
    {
        var first = await CreateTaskAsync(_owner);
        var second = await CreateTaskAsync(_owner);
        await _owner.PatchAsJsonAsync($"/api/tasks/{first}/assignee", new { userId = _memberId });
        await _owner.PatchAsJsonAsync($"/api/tasks/{second}/assignee", new { userId = _memberId });

        var id = (await ListAsync(_member)).GetProperty("items")[0].GetProperty("id").GetString();

        // A notificação de outra pessoa "não existe".
        await ApiTests.AssertErrorAsync(await _owner.PostAsync($"/api/notifications/{id}/read", null), HttpStatusCode.NotFound);

        Assert.Equal(HttpStatusCode.NoContent, (await _member.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        var list = await ListAsync(_member);
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());
        Assert.True(list.GetProperty("items")[0].GetProperty("read").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await _member.PostAsync("/api/notifications/read-all", null)).StatusCode);
        Assert.Equal(0, (await ListAsync(_member)).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Quem_sai_do_projeto_deixa_de_ver_os_avisos_dele()
    {
        var taskId = await CreateTaskAsync(_owner);
        await _owner.PatchAsJsonAsync($"/api/tasks/{taskId}/assignee", new { userId = _memberId });

        await _owner.DeleteAsync($"/api/projects/{_projectId}/members/{_memberId}");

        var list = await ListAsync(_member);
        Assert.Equal(0, list.GetProperty("unreadCount").GetInt32());
        Assert.Equal(0, list.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Aviso_chega_em_tempo_real_so_para_quem_foi_notificado()
    {
        var taskId = await CreateTaskAsync(_owner);

        await using var memberLive = await ConnectAsync(_member);
        await using var ownerLive = await ConnectAsync(_owner);
        var received = new TaskCompletionSource();
        var ownerReceived = false;
        memberLive.On<JsonElement>("Notified", _ => received.TrySetResult());
        ownerLive.On<JsonElement>("Notified", _ => ownerReceived = true);

        await _owner.PatchAsJsonAsync($"/api/tasks/{taskId}/assignee", new { userId = _memberId });

        await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(ownerReceived);
    }

    private async Task<HubConnection> ConnectAsync(HttpClient client)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/board", o =>
            {
                o.HttpMessageHandlerFactory = _ => db.Api.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling;
                o.AccessTokenProvider = () => Task.FromResult(client.DefaultRequestHeaders.Authorization?.Parameter);
            })
            .Build();

        await connection.StartAsync();

        return connection;
    }
}
