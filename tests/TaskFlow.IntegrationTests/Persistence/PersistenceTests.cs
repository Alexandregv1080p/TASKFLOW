using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public class PersistenceTests(DatabaseFixture db)
{
    private async Task<(User user, Project project)> SeedProjectAsync()
    {
        var user = new User("Ana", $"{Guid.NewGuid():N}@example.com", "hash");
        var project = new Project("Website", "Portfólio", user.Id);

        await using var context = db.CreateContext();
        context.AddRange(user, project);
        await context.SaveChangesAsync();

        return (user, project);
    }

    [Fact]
    public async Task Deve_salvar_e_ler_projeto_e_tarefa()
    {
        var (user, project) = await SeedProjectAsync();
        var task = new TaskItem(project.Id, 1, user.Id, "Login", "JWT", TaskPriority.High, new DateTime(2026, 10, 5));
        task.AssignTo(user.Id);
        task.ChangeStatus(TaskItemStatus.InProgress);

        await using (var context = db.CreateContext())
        {
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.Tasks.SingleAsync(t => t.Id == task.Id);

            Assert.Equal("Login", saved.Title);
            Assert.Equal(TaskItemStatus.InProgress, saved.Status);
            Assert.Equal(TaskPriority.High, saved.Priority);
            Assert.Equal(new DateTime(2026, 10, 5), saved.DueDate);
            Assert.Equal(user.Id, saved.AssignedToId);
            Assert.Equal(project.Id, saved.ProjectId);
            Assert.Equal(1, saved.Number);
            Assert.Equal(user.Id, saved.CreatedById);
        }
    }

    [Fact]
    public async Task Numero_da_tarefa_e_unico_no_projeto()
    {
        var (user, project) = await SeedProjectAsync();

        await using var context = db.CreateContext();
        context.Tasks.AddRange(
            new TaskItem(project.Id, 1, user.Id, "A", null, TaskPriority.Low, null),
            new TaskItem(project.Id, 1, user.Id, "B", null, TaskPriority.Low, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Excluir_projeto_deve_excluir_suas_tarefas()
    {
        var (user, project) = await SeedProjectAsync();

        await using (var context = db.CreateContext())
        {
            context.Tasks.Add(new TaskItem(project.Id, 1, user.Id, "Tarefa", null, TaskPriority.Low, null));
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            await context.Projects.Where(p => p.Id == project.Id).ExecuteDeleteAsync();

            Assert.False(await context.Tasks.AnyAsync(t => t.ProjectId == project.Id));
        }
    }

    [Fact]
    public async Task Email_deve_ser_unico()
    {
        var (user, _) = await SeedProjectAsync();

        await using var context = db.CreateContext();
        context.Users.Add(new User("Outra", user.Email, "hash"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Tarefa_nao_pode_referenciar_projeto_inexistente()
    {
        await using var context = db.CreateContext();
        context.Tasks.Add(new TaskItem(Guid.NewGuid(), 1, Guid.NewGuid(), "Órfã", null, TaskPriority.Low, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
