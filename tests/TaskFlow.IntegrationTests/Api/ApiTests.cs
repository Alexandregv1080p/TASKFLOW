using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class ApiTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await db.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<JsonElement> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "Website", description = "Portfólio" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Health_retorna_healthy()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/health");

        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Metrics_expoe_metricas_http_e_de_negocio()
    {
        var projectId = (await CreateProjectAsync()).GetProperty("id").GetString();
        await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "Medir", priority = "Low" });

        // Formato de texto do Prometheus, acessível sem token (o scraper não autentica).
        var metrics = await db.Api.CreateClient().GetStringAsync("/metrics");

        Assert.Contains("http_request_duration_seconds", metrics);
        Assert.Contains("taskflow_tasks_created", metrics);
        Assert.Contains("taskflow_projects_created", metrics);
    }

    [Fact]
    public async Task Crud_de_projeto()
    {
        var project = await CreateProjectAsync();
        var id = project.GetProperty("id").GetString();

        var updated = await _client.PutAsJsonAsync($"/api/projects/{id}", new { name = "Site novo" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/projects/{id}");
        Assert.Equal("Site novo", fetched.GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/projects/{id}")).StatusCode);
        await AssertErrorAsync(await _client.GetAsync($"/api/projects/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Fluxo_de_tarefa_com_enums_como_texto()
    {
        var projectId = (await CreateProjectAsync()).GetProperty("id").GetString();

        var created = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "Implementar autenticação", priority = "High", dueDate = "2026-10-05" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var task = await created.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = task.GetProperty("id").GetString();
        Assert.Equal("Todo", task.GetProperty("status").GetString());
        Assert.Equal("High", task.GetProperty("priority").GetString());

        var moved = await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/status", new { status = "InProgress" });
        Assert.Equal("InProgress", (await moved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var tasks = await _client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/tasks");
        Assert.Equal(1, tasks.GetArrayLength());
    }

    [Fact]
    public async Task Regra_de_dominio_violada_retorna_400()
    {
        var projectId = (await CreateProjectAsync()).GetProperty("id").GetString();
        var task = await (await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "Login", priority = "Low" })).Content.ReadFromJsonAsync<JsonElement>();

        var response = await _client.PatchAsJsonAsync($"/api/tasks/{task.GetProperty("id").GetString()}/status",
            new { status = "Done" });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{\"name\": \"\"}")]
    [InlineData("{\"description\": \"sem nome\"}")]
    [InlineData("{\"name\": \"x\", \"priority\": ")]
    public async Task Request_invalido_retorna_400_no_formato_padrao(string json)
    {
        var response = await _client.PostAsync("/api/projects",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Prioridade_desconhecida_retorna_400()
    {
        var projectId = (await CreateProjectAsync()).GetProperty("id").GetString();

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "Login", priority = "Urgentíssima" });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listagem_e_dashboard_trazem_contagens()
    {
        var projectId = (await CreateProjectAsync()).GetProperty("id").GetString();

        async Task<string?> AddTaskAsync(string priority) =>
            (await (await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "T", priority }))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        await AddTaskAsync("Low");
        var done = await AddTaskAsync("High");
        await _client.PatchAsJsonAsync($"/api/tasks/{done}/status", new { status = "InProgress" });
        await _client.PatchAsJsonAsync($"/api/tasks/{done}/status", new { status = "Done" });

        var projects = await _client.GetFromJsonAsync<JsonElement>("/api/projects");
        var summary = projects.EnumerateArray().Single(p => p.GetProperty("id").GetString() == projectId);
        Assert.Equal(2, summary.GetProperty("taskCount").GetInt32());
        Assert.Equal(1, summary.GetProperty("doneCount").GetInt32());

        // Usuário novo por teste: o dashboard contém exatamente os dados criados aqui.
        var dashboard = await _client.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, dashboard.GetProperty("totalProjects").GetInt32());
        Assert.Equal(2, dashboard.GetProperty("totalTasks").GetInt32());
        Assert.Equal(1, dashboard.GetProperty("tasksByStatus").GetProperty("done").GetInt32());
        Assert.Equal(1, dashboard.GetProperty("tasksByPriority").GetProperty("high").GetInt32());
        Assert.Equal(projectId, dashboard.GetProperty("recentProjects")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Tarefa_em_projeto_inexistente_retorna_404()
    {
        var response = await _client.PostAsJsonAsync($"/api/projects/{Guid.NewGuid()}/tasks",
            new { title = "Login", priority = "Low" });

        await AssertErrorAsync(response, HttpStatusCode.NotFound);
    }
}
