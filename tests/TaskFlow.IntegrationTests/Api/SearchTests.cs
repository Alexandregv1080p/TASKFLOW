using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class SearchTests(DatabaseFixture db)
{
    private static string[] Names(JsonElement result, string group, string field) =>
        result.GetProperty(group).EnumerateArray().Select(e => e.GetProperty(field).ToString()).ToArray();

    [Fact]
    public async Task Busca_tarefas_projetos_e_espacos_sem_diferenciar_maiusculas()
    {
        var me = await db.CreateAuthenticatedClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..8]; // isola este teste dos dados dos outros

        var space = await (await me.PostAsJsonAsync("/api/spaces", new { name = $"Espaço {tag}", templateKey = "blank" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var project = await (await me.PostAsJsonAsync("/api/projects",
                new { name = $"Portal {tag}", spaceId = space.GetProperty("id").GetString() }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString();
        await me.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = $"Revisar {tag} contrato", priority = "Low" });

        var result = await me.GetFromJsonAsync<JsonElement>($"/api/search?q={tag.ToUpperInvariant()}");

        Assert.Equal([$"Revisar {tag} contrato"], Names(result, "tasks", "title"));
        Assert.Equal([$"Portal {tag}"], Names(result, "projects", "name"));
        Assert.Equal($"Espaço {tag}", result.GetProperty("projects")[0].GetProperty("spaceName").GetString());
        Assert.Equal([$"Espaço {tag}"], Names(result, "spaces", "name"));
        Assert.Equal("Todo", result.GetProperty("tasks")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Busca_por_numero_da_tarefa_e_termo_curto()
    {
        var me = await db.CreateAuthenticatedClientAsync();
        var project = await (await me.PostAsJsonAsync("/api/projects", new { name = "Numeros" })).Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString();
        await me.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "Primeira", priority = "Low" });
        await me.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "Segunda", priority = "Low" });

        // Usuário novo: só as tarefas dele aparecem.
        Assert.Equal(["Segunda"], Names(await me.GetFromJsonAsync<JsonElement>("/api/search?q=NUM-2"), "tasks", "title"));
        Assert.Equal(["Primeira"], Names(await me.GetFromJsonAsync<JsonElement>("/api/search?q=%231"), "tasks", "title"));

        // Uma letra só não busca nada (evita varrer tudo a cada tecla).
        var tooShort = await me.GetFromJsonAsync<JsonElement>("/api/search?q=p");
        Assert.Equal(0, tooShort.GetProperty("tasks").GetArrayLength());
        Assert.Equal(0, tooShort.GetProperty("projects").GetArrayLength());
    }

    [Fact]
    public async Task Nao_encontra_nada_de_projetos_de_outras_pessoas()
    {
        var owner = await db.CreateAuthenticatedClientAsync("Dona");
        var tag = Guid.NewGuid().ToString("N")[..8];
        var project = await (await owner.PostAsJsonAsync("/api/projects", new { name = $"Segredo {tag}" })).Content.ReadFromJsonAsync<JsonElement>();
        await owner.PostAsJsonAsync($"/api/projects/{project.GetProperty("id").GetString()}/tasks", new { title = $"Plano {tag}", priority = "Low" });
        await owner.PostAsJsonAsync("/api/spaces", new { name = $"Diretoria {tag}", templateKey = "blank" });

        var intruder = await db.CreateAuthenticatedClientAsync("Intruso");
        var result = await intruder.GetFromJsonAsync<JsonElement>($"/api/search?q={tag}");

        Assert.Equal(0, result.GetProperty("tasks").GetArrayLength());
        Assert.Equal(0, result.GetProperty("projects").GetArrayLength());
        Assert.Equal(0, result.GetProperty("spaces").GetArrayLength());
    }
}
