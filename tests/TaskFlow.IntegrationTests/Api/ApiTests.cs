using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class ApiTests(DatabaseFixture db)
{
    private readonly HttpClient _client = db.Api.CreateClient();

    private async Task<JsonElement> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "Website", description = "Portfólio" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status)
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
    public async Task Tarefa_em_projeto_inexistente_retorna_404()
    {
        var response = await _client.PostAsJsonAsync($"/api/projects/{Guid.NewGuid()}/tasks",
            new { title = "Login", priority = "Low" });

        await AssertErrorAsync(response, HttpStatusCode.NotFound);
    }
}
