using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Antivirus;
using TaskFlow.IntegrationTests.Api;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Antivirus;

[Collection(DatabaseCollection.Name)]
public class AntivirusTests(DatabaseFixture db)
{
    private static MemoryStream Bytes(string text) => new(Encoding.ASCII.GetBytes(text));

    [Fact]
    public async Task Cliente_fala_o_protocolo_INSTREAM()
    {
        await using var clamd = new FakeClamd();
        var scanner = new ClamAvScanner(new AntivirusOptions { Host = "127.0.0.1", Port = clamd.Port });

        // Maior que um bloco (64 KB): o arquivo chega inteiro, remontado a partir dos blocos.
        var big = new string('a', 200_000);
        Assert.True((await scanner.ScanAsync(Bytes(big))).Clean);
        Assert.Equal(200_000, clamd.LastReceived.Length);

        var infected = await scanner.ScanAsync(Bytes("prefixo " + FakeClamd.Eicar));
        Assert.False(infected.Clean);
        Assert.Equal("Eicar-Test-Signature", infected.Threat);
    }

    [Fact]
    public async Task Antivirus_fora_do_ar_nao_deixa_passar()
    {
        // Porta livre e ninguém escutando.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var scanner = new ClamAvScanner(new AntivirusOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 5 });

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => scanner.ScanAsync(Bytes("qualquer coisa")));
    }

    [Fact]
    public async Task Upload_passa_pelo_antivirus()
    {
        await using var clamd = new FakeClamd();
        await using var api = db.Api.WithWebHostBuilder(b => b
            .UseSetting("Antivirus:Host", "127.0.0.1")
            .UseSetting("Antivirus:Port", clamd.Port.ToString()));
        var (client, taskId) = await ProjectWithTaskAsync(api);

        Assert.Equal(HttpStatusCode.Created, (await UploadAsync(client, taskId, "limpo.txt", "relatório")).StatusCode);

        var blocked = await UploadAsync(client, taskId, "eicar.txt", FakeClamd.Eicar);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Contains("blocked by the antivirus: Eicar-Test-Signature", await blocked.Content.ReadAsStringAsync());

        // O bloqueado não ficou nem no banco nem no disco: só o limpo aparece.
        var list = await client.GetFromJsonAsync<JsonElement>($"/api/tasks/{taskId}/attachments");
        Assert.Equal(["limpo.txt"], list.EnumerateArray().Select(a => a.GetProperty("fileName").GetString()));
    }

    [Fact]
    public async Task Upload_com_antivirus_fora_do_ar_responde_503()
    {
        await using var api = db.Api.WithWebHostBuilder(b => b
            .UseSetting("Antivirus:Host", "127.0.0.1")
            .UseSetting("Antivirus:Port", "1")
            .UseSetting("Antivirus:TimeoutSeconds", "5"));
        var (client, taskId) = await ProjectWithTaskAsync(api);

        await ApiTests.AssertErrorAsync(await UploadAsync(client, taskId, "nota.txt", "oi"), HttpStatusCode.ServiceUnavailable);
    }

    // Contra o ClamAV de verdade (docker compose --profile antivirus up -d), quando TASKFLOW_TEST_CLAMAV_HOST existe.
    [ClamAvFact]
    public async Task ClamAV_de_verdade_reconhece_o_EICAR()
    {
        var scanner = new ClamAvScanner(new AntivirusOptions
        {
            Host = Environment.GetEnvironmentVariable("TASKFLOW_TEST_CLAMAV_HOST"),
            Port = int.Parse(Environment.GetEnvironmentVariable("TASKFLOW_TEST_CLAMAV_PORT") ?? "3310"),
        });

        Assert.True((await scanner.ScanAsync(Bytes("um arquivo comum"))).Clean);

        var result = await scanner.ScanAsync(Bytes(FakeClamd.Eicar));
        Assert.False(result.Clean);
        Assert.Contains("Eicar", result.Threat);
    }

    private static async Task<(HttpClient Client, string TaskId)> ProjectWithTaskAsync(WebApplicationFactory<Program> api)
    {
        var client = api.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/auth/register",
                new { name = "Dona Antivirus", email = $"{Guid.NewGuid():N}@example.com", password = "senha-segura-123" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());

        var project = await (await client.PostAsJsonAsync("/api/projects", new { name = "Antivirus" })).Content.ReadFromJsonAsync<JsonElement>();
        var task = await (await client.PostAsJsonAsync($"/api/projects/{project.GetProperty("id").GetString()}/tasks", new { title = "T", priority = "Low" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        return (client, task.GetProperty("id").GetString()!);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, string taskId, string name, string text)
    {
        var file = new ByteArrayContent(Encoding.ASCII.GetBytes(text));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        return client.PostAsync($"/api/tasks/{taskId}/attachments", new MultipartFormDataContent { { file, "file", name } });
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ClamAvFactAttribute : FactAttribute
{
    public ClamAvFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TASKFLOW_TEST_CLAMAV_HOST")))
            Skip = "Sem ClamAV configurado: defina TASKFLOW_TEST_CLAMAV_HOST (docker compose --profile antivirus up -d).";
    }
}
