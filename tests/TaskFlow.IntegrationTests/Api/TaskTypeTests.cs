using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class TaskTypeTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _owner = null!;
    private HttpClient _member = null!;
    private string _memberId = null!;
    private string _projectId = null!;

    public async Task InitializeAsync()
    {
        _owner = await db.CreateAuthenticatedClientAsync("Dona");
        _member = await db.CreateAuthenticatedClientAsync("Membro");

        var me = await _member.GetFromJsonAsync<JsonElement>("/api/auth/me");
        _memberId = me.GetProperty("id").GetString()!;

        var project = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "App" })).Content.ReadFromJsonAsync<JsonElement>();
        _projectId = project.GetProperty("id").GetString()!;
        (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email = me.GetProperty("email").GetString() }))
            .EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<JsonElement[]> TypesAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/projects/{_projectId}/task-types")).EnumerateArray().ToArray();

    private async Task<JsonElement> CreateTaskAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Projeto_novo_vem_com_tipos_padrao_e_so_o_dono_gerencia()
    {
        Assert.Equal(["Desenvolvimento", "Bug", "Integração"], (await TypesAsync(_member)).Select(t => t.GetProperty("name").GetString()));

        // Membro vê, mas não cria (403).
        await ApiTests.AssertErrorAsync(
            await _member.PostAsJsonAsync($"/api/projects/{_projectId}/task-types", new { name = "Melhoria", color = "#22A06B" }),
            HttpStatusCode.Forbidden);

        var created = await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/task-types", new { name = "Melhoria", color = "#22a06b" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var melhoria = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#22A06B", melhoria.GetProperty("color").GetString());

        // Nome repetido (sem diferenciar maiúsculas) e cor inválida.
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/task-types", new { name = "bug", color = "#000000" }),
            HttpStatusCode.Conflict);
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/task-types", new { name = "Pesquisa", color = "azul" }),
            HttpStatusCode.BadRequest);

        var id = melhoria.GetProperty("id").GetString();
        var renamed = await _owner.PutAsJsonAsync($"/api/task-types/{id}", new { name = "Melhoria contínua", color = "#22A06B" });
        Assert.Equal("Melhoria contínua", (await renamed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());
        Assert.Equal(4, (await TypesAsync(_owner)).Length);

        // Quem não está no projeto não vê nem os tipos.
        var stranger = await db.CreateAuthenticatedClientAsync("Intruso");
        await ApiTests.AssertErrorAsync(await stranger.GetAsync($"/api/projects/{_projectId}/task-types"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tarefa_criada_com_tipo_e_responsavel_e_tipo_trocado_depois()
    {
        var types = await TypesAsync(_owner);
        var bug = types.Single(t => t.GetProperty("name").GetString() == "Bug").GetProperty("id").GetString();
        var integracao = types.Single(t => t.GetProperty("name").GetString() == "Integração").GetProperty("id").GetString();

        var task = await CreateTaskAsync(_owner, new { title = "Login quebrado", priority = "High", typeId = bug, assignedToId = _memberId });
        var taskId = task.GetProperty("id").GetString();
        Assert.Equal(bug, task.GetProperty("typeId").GetString());
        Assert.Equal(_memberId, task.GetProperty("assignedToId").GetString());

        // Quem recebeu a tarefa já na criação é avisado.
        var notifications = await _member.GetFromJsonAsync<JsonElement>("/api/notifications");
        Assert.Equal("Assigned", notifications.GetProperty("items")[0].GetProperty("type").GetString());

        var changed = await _member.PatchAsJsonAsync($"/api/tasks/{taskId}/type", new { typeId = integracao });
        Assert.Equal(integracao, (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("typeId").GetString());

        var history = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{taskId}/history");
        var typeEvent = history.EnumerateArray().Single(e => e.GetProperty("type").GetString() == "TypeChanged");
        Assert.Equal(bug, typeEvent.GetProperty("oldValue").GetString());
        Assert.Equal(integracao, typeEvent.GetProperty("newValue").GetString());

        // Excluir o tipo deixa a tarefa sem tipo, sem apagá-la.
        Assert.Equal(HttpStatusCode.NoContent, (await _owner.DeleteAsync($"/api/task-types/{integracao}")).StatusCode);
        var after = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{taskId}");
        Assert.Equal(JsonValueKind.Null, after.GetProperty("typeId").ValueKind);
    }

    [Fact]
    public async Task Tipo_ou_responsavel_de_fora_do_projeto_nao_entram()
    {
        var other = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Outro" })).Content.ReadFromJsonAsync<JsonElement>();
        var otherTypes = await _owner.GetFromJsonAsync<JsonElement>($"/api/projects/{other.GetProperty("id").GetString()}/task-types");
        var foreignType = otherTypes[0].GetProperty("id").GetString();

        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "X", priority = "Low", typeId = foreignType }),
            HttpStatusCode.NotFound);

        var outsider = await (await db.CreateAuthenticatedClientAsync("Fora")).GetFromJsonAsync<JsonElement>("/api/auth/me");
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks",
                new { title = "X", priority = "Low", assignedToId = outsider.GetProperty("id").GetString() }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Subtarefas_um_nivel_no_mesmo_projeto_e_somem_com_a_tarefa_pai()
    {
        var parent = await CreateTaskAsync(_owner, new { title = "Checkout", priority = "Medium" });
        var parentId = parent.GetProperty("id").GetString();

        var child = await CreateTaskAsync(_member, new { title = "Validar cartão", priority = "Low", parentId });
        Assert.Equal(parentId, child.GetProperty("parentId").GetString());

        // Subtarefa de subtarefa: não.
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks",
                new { title = "Neta", priority = "Low", parentId = child.GetProperty("id").GetString() }),
            HttpStatusCode.BadRequest);

        // Pai de outro projeto: não.
        var other = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Outro" })).Content.ReadFromJsonAsync<JsonElement>();
        await ApiTests.AssertErrorAsync(
            await _owner.PostAsJsonAsync($"/api/projects/{other.GetProperty("id").GetString()}/tasks",
                new { title = "Fora", priority = "Low", parentId }),
            HttpStatusCode.BadRequest);

        // Tarefa existente vira subtarefa, e volta a ser comum; as duas mudanças entram no histórico.
        var loose = await CreateTaskAsync(_owner, new { title = "Emitir nota", priority = "Low" });
        var looseId = loose.GetProperty("id").GetString();
        var moved = await _member.PatchAsJsonAsync($"/api/tasks/{looseId}/parent", new { parentId });
        Assert.Equal(parentId, (await moved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("parentId").GetString());

        // Quem tem subtarefas não vira subtarefa; nem a tarefa vira pai de si mesma.
        await ApiTests.AssertErrorAsync(
            await _owner.PatchAsJsonAsync($"/api/tasks/{parentId}/parent", new { parentId = looseId }), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(
            await _owner.PatchAsJsonAsync($"/api/tasks/{looseId}/parent", new { parentId = looseId }), HttpStatusCode.BadRequest);

        var detached = await _owner.PatchAsJsonAsync($"/api/tasks/{looseId}/parent", new { parentId = (string?)null });
        Assert.Equal(JsonValueKind.Null, (await detached.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("parentId").ValueKind);
        var events = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{looseId}/history");
        Assert.Equal(2, events.EnumerateArray().Count(e => e.GetProperty("type").GetString() == "ParentChanged"));

        Assert.Equal(HttpStatusCode.NoContent, (await _owner.DeleteAsync($"/api/tasks/{parentId}")).StatusCode);
        await ApiTests.AssertErrorAsync(await _owner.GetAsync($"/api/tasks/{child.GetProperty("id").GetString()}"), HttpStatusCode.NotFound);
    }
}
