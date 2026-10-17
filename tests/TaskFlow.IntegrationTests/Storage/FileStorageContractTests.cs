using System.Text;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Storage;

namespace TaskFlow.IntegrationTests.Storage;

// As duas implementações de IFileStorage precisam se comportar igual: as mesmas verificações rodam nas duas.
// O S3 entra quando há um configurado (o SeaweedFS do compose, perfil "s3", ou a AWS); senão, só a pasta local,
// e o teste S3_configurado aparece como pulado para isso não passar despercebido.
public class FileStorageContractTests
{
    private static readonly string? S3Url = Environment.GetEnvironmentVariable("TASKFLOW_TEST_S3_URL");

    public static TheoryData<string> Storages()
    {
        var data = new TheoryData<string> { "local" };
        if (!string.IsNullOrEmpty(S3Url))
            data.Add("s3");

        return data;
    }

    private static IFileStorage Create(string kind) => kind == "s3"
        ? new S3FileStorage(new S3Options
        {
            ServiceUrl = S3Url,
            AccessKey = Environment.GetEnvironmentVariable("TASKFLOW_TEST_S3_ACCESS_KEY") ?? "taskflow",
            SecretKey = Environment.GetEnvironmentVariable("TASKFLOW_TEST_S3_SECRET_KEY"),
            Bucket = "taskflow-tests",
        })
        : new LocalFileStorage(Path.Combine(Path.GetTempPath(), "taskflow-storage-contract"));

    private static MemoryStream Text(string value) => new(Encoding.UTF8.GetBytes(value));

    private static async Task<string> ReadAsync(IFileStorage storage, string key)
    {
        await using var stream = await storage.OpenReadAsync(key);
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    [Theory]
    [MemberData(nameof(Storages))]
    public async Task Grava_le_e_apaga(string kind)
    {
        var storage = Create(kind);
        var key = $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

        await storage.SaveAsync(key, Text("conteúdo do anexo"));
        Assert.Equal("conteúdo do anexo", await ReadAsync(storage, key));

        await storage.DeleteAsync(key);
        await Assert.ThrowsAsync<FileNotFoundException>(() => ReadAsync(storage, key));

        // Nunca existiu (nem a pasta, no caso local): o mesmo erro.
        await Assert.ThrowsAsync<FileNotFoundException>(() => ReadAsync(storage, $"{Guid.NewGuid()}/nada"));
    }

    [Theory]
    [MemberData(nameof(Storages))]
    public async Task Apaga_por_prefixo_sem_tocar_no_vizinho(string kind)
    {
        var storage = Create(kind);
        var project = Guid.NewGuid();
        var (task, other) = (Guid.NewGuid(), Guid.NewGuid());

        await storage.SaveAsync($"{project}/{task}/a", Text("a"));
        await storage.SaveAsync($"{project}/{task}/b", Text("b"));
        await storage.SaveAsync($"{project}/{other}/c", Text("c"));

        await storage.DeletePrefixAsync($"{project}/{task}");

        await Assert.ThrowsAnyAsync<Exception>(() => ReadAsync(storage, $"{project}/{task}/a"));
        await Assert.ThrowsAnyAsync<Exception>(() => ReadAsync(storage, $"{project}/{task}/b"));
        Assert.Equal("c", await ReadAsync(storage, $"{project}/{other}/c"));

        // Prefixo que não existe não é erro.
        await storage.DeletePrefixAsync($"{Guid.NewGuid()}");
    }

    [Fact]
    public async Task Pasta_local_recusa_chave_fora_da_raiz()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create("local").SaveAsync("../fora.txt", new MemoryStream([1])));
    }

    [S3Fact]
    public void S3_configurado() => Assert.Contains("s3", Storages().Select(row => (string)row[0]));
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class S3FactAttribute : FactAttribute
{
    public S3FactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TASKFLOW_TEST_S3_URL")))
            Skip = "Sem S3 configurado: defina TASKFLOW_TEST_S3_URL e TASKFLOW_TEST_S3_SECRET_KEY (docker compose --profile s3 up -d).";
    }
}
