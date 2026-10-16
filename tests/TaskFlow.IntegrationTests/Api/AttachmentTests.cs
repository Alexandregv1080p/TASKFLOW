using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class AttachmentTests(DatabaseFixture db) : IAsyncLifetime
{
    private HttpClient _owner = null!;
    private HttpClient _member = null!;
    private HttpClient _viewer = null!;
    private string _projectId = null!;
    private string _taskId = null!;

    // Projeto com uma tarefa, uma pessoa membro e uma visualizadora.
    public async Task InitializeAsync()
    {
        _owner = await db.CreateAuthenticatedClientAsync("Dona Anexos");
        _member = await db.CreateAuthenticatedClientAsync("Membro Anexos");
        _viewer = await db.CreateAuthenticatedClientAsync("Visualizador Anexos");

        var project = await (await _owner.PostAsJsonAsync("/api/projects", new { name = "Anexos" })).Content.ReadFromJsonAsync<JsonElement>();
        _projectId = project.GetProperty("id").GetString()!;
        var task = await (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Bug do login", priority = "High" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        _taskId = task.GetProperty("id").GetString()!;

        foreach (var (client, role) in new[] { (_member, "Member"), (_viewer, "Viewer") })
        {
            var email = (await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("email").GetString();
            (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email, role })).EnsureSuccessStatusCode();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, string taskId, string fileName, byte[] content, string type = "text/plain")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);

        return client.PostAsync($"/api/tasks/{taskId}/attachments", new MultipartFormDataContent { { file, "file", fileName } });
    }

    [Fact]
    public async Task Anexar_listar_baixar_e_excluir()
    {
        var content = Encoding.UTF8.GetBytes("passo 1: abrir o login\npasso 2: erro 500");

        // O tipo vem da extensão, não do que o navegador declarou.
        var uploaded = await UploadAsync(_member, _taskId, "C:\\fakepath\\passos.txt", content, "application/x-msdownload");
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var attachment = await uploaded.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("passos.txt", attachment.GetProperty("fileName").GetString());
        Assert.Equal("text/plain", attachment.GetProperty("contentType").GetString());
        Assert.Equal(content.Length, attachment.GetProperty("sizeBytes").GetInt64());
        var id = attachment.GetProperty("id").GetString();

        // Visualizador lista e baixa: sempre como download, sem o navegador adivinhar o tipo.
        var list = await _viewer.GetFromJsonAsync<JsonElement>($"/api/tasks/{_taskId}/attachments");
        Assert.Equal(1, list.GetArrayLength());

        var download = await _viewer.GetAsync($"/api/attachments/{id}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(content, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());

        // Entra no histórico da tarefa.
        var history = await _owner.GetFromJsonAsync<JsonElement>($"/api/tasks/{_taskId}/history");
        Assert.Contains(history.EnumerateArray(), e =>
            e.GetProperty("type").GetString() == "AttachmentAdded" && e.GetProperty("newValue").GetString() == "passos.txt");

        // Visualizador não exclui; quem não está no projeto nem enxerga o anexo.
        await ApiTests.AssertErrorAsync(await _viewer.DeleteAsync($"/api/attachments/{id}"), HttpStatusCode.Forbidden);
        var stranger = await db.CreateAuthenticatedClientAsync("Intruso Anexos");
        await ApiTests.AssertErrorAsync(await stranger.GetAsync($"/api/attachments/{id}"), HttpStatusCode.NotFound);

        // O dono do projeto exclui o anexo de outra pessoa, e o arquivo sai do disco.
        var file = Path.Combine(DatabaseFixture.StoragePath, _projectId, _taskId, id!);
        Assert.True(File.Exists(file));
        Assert.Equal(HttpStatusCode.NoContent, (await _owner.DeleteAsync($"/api/attachments/{id}")).StatusCode);
        Assert.False(File.Exists(file));
        await ApiTests.AssertErrorAsync(await _owner.GetAsync($"/api/attachments/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Recusa_tipo_perigoso_arquivo_grande_e_visualizador()
    {
        await ApiTests.AssertErrorAsync(await UploadAsync(_member, _taskId, "fotos.exe", [1, 2, 3]), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(await UploadAsync(_member, _taskId, "pagina.html", "<script>"u8.ToArray()), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(await UploadAsync(_member, _taskId, "vazio.txt", []), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(
            await UploadAsync(_member, _taskId, "grande.pdf", new byte[10 * 1024 * 1024 + 1], "application/pdf"), HttpStatusCode.BadRequest);

        await ApiTests.AssertErrorAsync(await UploadAsync(_viewer, _taskId, "nota.txt", "oi"u8.ToArray()), HttpStatusCode.Forbidden);

        // Nada disso deixou arquivo no disco.
        Assert.False(Directory.Exists(Path.Combine(DatabaseFixture.StoragePath, _projectId, _taskId)));
    }

    [Fact]
    public async Task So_quem_anexou_ou_o_dono_exclui_e_excluir_a_tarefa_apaga_os_arquivos()
    {
        var other = await db.CreateAuthenticatedClientAsync("Outro Membro");
        var email = (await other.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("email").GetString();
        await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/members", new { email });

        var attachment = await (await UploadAsync(_member, _taskId, "print.png", [137, 80, 78, 71], "image/png"))
            .Content.ReadFromJsonAsync<JsonElement>();
        await ApiTests.AssertErrorAsync(
            await other.DeleteAsync($"/api/attachments/{attachment.GetProperty("id").GetString()}"), HttpStatusCode.Forbidden);

        // Subtarefa com anexo: excluir a tarefa-pai leva as duas pastas.
        var sub = await (await _owner.PostAsJsonAsync($"/api/projects/{_projectId}/tasks", new { title = "Sub", priority = "Low", parentId = _taskId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var subId = sub.GetProperty("id").GetString()!;
        await UploadAsync(_member, subId, "log.log", "erro"u8.ToArray());

        Assert.True(Directory.Exists(Path.Combine(DatabaseFixture.StoragePath, _projectId, _taskId)));
        Assert.True(Directory.Exists(Path.Combine(DatabaseFixture.StoragePath, _projectId, subId)));

        Assert.Equal(HttpStatusCode.NoContent, (await _owner.DeleteAsync($"/api/tasks/{_taskId}")).StatusCode);
        Assert.False(Directory.Exists(Path.Combine(DatabaseFixture.StoragePath, _projectId, _taskId)));
        Assert.False(Directory.Exists(Path.Combine(DatabaseFixture.StoragePath, _projectId, subId)));
    }
}
