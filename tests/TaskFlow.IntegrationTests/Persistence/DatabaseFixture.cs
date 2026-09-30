using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests.Persistence;

// Recria um banco exclusivo de testes a partir das migrations, uma vez por execução,
// e sobe a API em memória apontando para ele.
public class DatabaseFixture : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TASKFLOW_TEST_CONNECTION")
        ?? "Host=localhost;Port=5436;Database=taskflow_tests;Username=taskflow;Password=taskflow";

    public WebApplicationFactory<Program> Api { get; } = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Default", ConnectionString));

    // Cliente HTTP já autenticado como um usuário novo e exclusivo do teste.
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string name = "Ana")
    {
        var client = Api.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { name, email = $"{Guid.NewGuid():N}@example.com", password = "senha-segura-123" });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());

        return client;
    }

    public TaskFlowDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TaskFlowDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await Api.DisposeAsync();
}

// Todas as classes que usam o banco compartilham um único fixture e rodam em sequência.
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
