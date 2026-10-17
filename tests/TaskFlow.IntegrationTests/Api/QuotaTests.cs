using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class QuotaTests(DatabaseFixture db)
{
    private const int KB = 1024;

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, string taskId, string name, int size)
    {
        var file = new ByteArrayContent(new byte[size]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        return client.PostAsync($"/api/tasks/{taskId}/attachments", new MultipartFormDataContent { { file, "file", name } });
    }

    [Fact]
    public async Task Projeto_tem_cota_de_espaco_para_anexos()
    {
        // A mesma API, com cota de 1 MB por projeto (em vez de 100 MB) para o teste não precisar enviar muito.
        await using var api = db.Api.WithWebHostBuilder(b => b.UseSetting("Attachments:ProjectQuotaMb", "1"));
        var client = api.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/auth/register",
                new { name = "Dona Cota", email = $"{Guid.NewGuid():N}@example.com", password = "senha-segura-123" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());

        var project = await (await client.PostAsJsonAsync("/api/projects", new { name = "Cota" })).Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString();
        var first = await (await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "A", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title = "B", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var usage = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/storage");
        Assert.Equal(0, usage.GetProperty("usedBytes").GetInt64());
        Assert.Equal(1024 * KB, usage.GetProperty("quotaBytes").GetInt64());

        // A cota soma as tarefas do projeto: 600 KB numa tarefa e mais 600 KB na outra passaria de 1 MB.
        var kept = await (await UploadAsync(client, first.GetProperty("id").GetString()!, "a.pdf", 600 * KB))
            .Content.ReadFromJsonAsync<JsonElement>();
        var blocked = await UploadAsync(client, second.GetProperty("id").GetString()!, "b.pdf", 600 * KB);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Contains("0.6 MB of 1 MB", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        usage = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/storage");
        Assert.Equal(600 * KB, usage.GetProperty("usedBytes").GetInt64());

        // Excluir libera espaço.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/attachments/{kept.GetProperty("id").GetString()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await UploadAsync(client, second.GetProperty("id").GetString()!, "b.pdf", 600 * KB)).StatusCode);
    }
}
