using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskFlow.Application.Projects;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class ExportTests(DatabaseFixture db)
{
    [Fact]
    public async Task Exporta_as_tarefas_em_csv_para_o_excel()
    {
        var owner = await db.CreateAuthenticatedClientAsync("Dona Export");
        var project = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Portal do Cliente" })).Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString();
        var types = await owner.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/task-types");
        var me = await owner.GetFromJsonAsync<JsonElement>("/api/auth/me");

        var parent = await (await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Login; \"novo\"",
            description = "Linha 1\nLinha 2",
            priority = "High",
            dueDate = "2026-11-30",
            typeId = types[1].GetProperty("id").GetString(),
            assignedToId = me.GetProperty("id").GetString(),
        })).Content.ReadFromJsonAsync<JsonElement>();
        await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "=HYPERLINK(\"http://x\")", priority = "Low", parentId = parent.GetProperty("id").GetString() });

        // Visualizador também exporta.
        var viewer = await db.CreateAuthenticatedClientAsync("Cliente Export");
        var viewerEmail = (await viewer.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("email").GetString();
        await owner.PostAsJsonAsync($"/api/projects/{projectId}/members", new { email = viewerEmail, role = "Viewer" });

        var response = await viewer.GetAsync($"/api/projects/{projectId}/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("taskflow-pdc-", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);

        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Chave;Título;Tipo;Status;Prioridade;Responsável;Criador;Tarefa-pai;Prazo;Criada em (UTC);Descrição", lines[0]);
        Assert.StartsWith("PDC-1;\"Login; \"\"novo\"\"\";Bug;A fazer;Alta;Dona Export;Dona Export;;2026-11-30;", lines[1]);
        Assert.EndsWith(";\"Linha 1\nLinha 2\"", lines[1]);
        Assert.StartsWith("PDC-2;\"'=HYPERLINK(\"\"http://x\"\")\";;A fazer;Baixa;;Dona Export;PDC-1;;", lines[2]);

        // Quem não está no projeto não exporta (404, o projeto "não existe").
        var stranger = await db.CreateAuthenticatedClientAsync("Intruso Export");
        await ApiTests.AssertErrorAsync(await stranger.GetAsync($"/api/projects/{projectId}/export"), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("simples", "simples")]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("diz \"oi\"", "\"diz \"\"oi\"\"\"")]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("-10", "'-10")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    public void Campo_do_csv(string? value, string expected) => Assert.Equal(expected, ProjectExportService.Field(value));
}
