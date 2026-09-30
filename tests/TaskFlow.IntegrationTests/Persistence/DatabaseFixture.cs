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
