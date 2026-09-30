using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests.Persistence;

// Recria um banco exclusivo de testes a partir das migrations, uma vez por execução.
public class DatabaseFixture : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TASKFLOW_TEST_CONNECTION")
        ?? "Host=localhost;Port=5436;Database=taskflow_tests;Username=taskflow;Password=taskflow";

    public TaskFlowDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TaskFlowDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
