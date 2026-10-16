using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class ViewerTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _owner = null!;
    private HttpClient _viewer = null!;
    private string _viewerId = null!;
    private string _viewerEmail = null!;
    private string _projectId = null!;
    private string _taskId = null!;

    // Projeto com uma tarefa, compartilhado com alguém só para leitura.
    public async Task InitializeAsync()
    {
        _owner = await db.CreateAuthenticatedClientAsync("Dona");
        _viewer = await db.CreateAuthenticatedClientAsync("Cliente");

        var me = await _viewer.GetFromJsonAsync<JsonElement>("/api/auth/me");
        _viewerId = me.GetProperty("id").GetString()!;
        _viewerEmail = me.GetProperty("email").GetString()!;

        var project = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Portal" })).Content.ReadFromJsonAsync<JsonElement>();
        _projectId = project.GetProperty("id").GetString()!;
        var task = await (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Home", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        _taskId = task.GetProperty("id").GetString()!;

        var added = await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email = _viewerEmail, role = "Viewer" });
        Assert.Equal("Viewer", (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Visualizador_le_tudo_mas_nao_altera()
    {
        // Lê: quadro, tarefa, comentários, histórico, pessoas. Estrela é pessoal e vale.
        Assert.Equal(1, (await _viewer.GetFromJsonAsync<JsonElement>($"/api/projects/{_projectId}/tasks")).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await _viewer.GetAsync($"/api/tasks/{_taskId}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _viewer.PutAsync($"/api/tasks/{_taskId}/star", null)).StatusCode);

        var people = await _viewer.GetFromJsonAsync<JsonElement>($"/api/projects/{_projectId}/members");
        Assert.Contains(people.EnumerateArray(), p => p.GetProperty("role").GetString() == "Owner");
        Assert.Contains(people.EnumerateArray(), p => p.GetProperty("role").GetString() == "Viewer");

        // Não altera nada (403: ele sabe que o projeto existe).
        await ApiTests.AssertErrorAsync(
            await _viewer.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "X", priority = "Low" }), HttpStatusCode.Forbidden);
        await ApiTests.AssertErrorAsync(
            await _viewer.PatchAsJsonAsync($"/api/tasks/{_taskId}/status", new { status = "InProgress" }), HttpStatusCode.Forbidden);
        await ApiTests.AssertErrorAsync(
            await _viewer.PostAsJsonAsync($"/api/tasks/{_taskId}/comments", new { body = "Oi" }), HttpStatusCode.Forbidden);
        await ApiTests.AssertErrorAsync(await _viewer.DeleteAsync($"/api/tasks/{_taskId}"), HttpStatusCode.Forbidden);

        // E não pode ser responsável por tarefa.
        await ApiTests.AssertErrorAsync(
            await _owner.PatchAsJsonAsync($"/api/tasks/{_taskId}/assignee", new { userId = _viewerId }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task So_o_dono_troca_papeis_e_quem_vira_visualizador_perde_as_tarefas()
    {
        // Vira membro: passa a trabalhar e recebe a tarefa.
        var promoted = await _owner.PatchAsJsonAsync($"/api/projects/{_projectId}/members/{_viewerId}", new { role = "Member" });
        Assert.Equal("Member", (await promoted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.OK,
            (await _viewer.PatchAsJsonAsync($"/api/tasks/{_taskId}/status", new { status = "InProgress" })).StatusCode);
        await _owner.PatchAsJsonAsync($"/api/tasks/{_taskId}/assignee", new { userId = _viewerId });

        // Membro não troca papel de ninguém, nem o próprio.
        await ApiTests.AssertErrorAsync(
            await _viewer.PatchAsJsonAsync($"/api/projects/{_projectId}/members/{_viewerId}", new { role = "Member" }), HttpStatusCode.Forbidden);
        // Não existe segundo dono.
        await ApiTests.AssertErrorAsync(
            await _owner.PatchAsJsonAsync($"/api/projects/{_projectId}/members/{_viewerId}", new { role = "Owner" }), HttpStatusCode.BadRequest);

        // Volta a visualizador: a tarefa fica sem responsável, com registro no histórico.
        await _owner.PatchAsJsonAsync($"/api/projects/{_projectId}/members/{_viewerId}", new { role = "Viewer" });
        var task = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{_taskId}");
        Assert.Equal(JsonValueKind.Null, task.GetProperty("assignedToId").ValueKind);

        var history = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{_taskId}/history");
        Assert.Equal(2, history.EnumerateArray().Count(e => e.GetProperty("type").GetString() == "AssigneeChanged"));
    }
}
