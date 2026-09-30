using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Application;

[Collection(DatabaseCollection.Name)]
public class ServiceTests(DatabaseFixture db)
{
    private async Task<User> SeedUserAsync()
    {
        var user = new User("Ana", $"{Guid.NewGuid():N}@example.com", "hash");

        await using var context = db.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task Ciclo_completo_de_projeto()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var service = new ProjectService(context);

        var created = await service.CreateAsync(user.Id, new CreateProjectRequest("Website", null));
        var updated = await service.UpdateAsync(created.Id, new UpdateProjectRequest("Site novo", "Descrição"));

        Assert.Equal("Site novo", updated.Name);
        Assert.Contains(await service.ListAsync(), p => p.Id == created.Id);

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(created.Id));
    }

    [Fact]
    public async Task Criar_projeto_com_dono_inexistente_retorna_not_found()
    {
        await using var context = db.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ProjectService(context).CreateAsync(Guid.NewGuid(), new CreateProjectRequest("Website", null)));
    }

    [Fact]
    public async Task Ciclo_completo_de_tarefa()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await new ProjectService(context).CreateAsync(user.Id, new CreateProjectRequest("Website", null));
        var service = new TaskService(context);

        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));
        await service.UpdateAsync(task.Id, new UpdateTaskRequest("Login JWT", "Detalhes", TaskPriority.High, new DateTime(2026, 10, 5)));
        await service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        await service.AssignAsync(task.Id, new AssignTaskRequest(user.Id));

        var saved = Assert.Single(await service.ListByProjectAsync(project.Id));
        Assert.Equal("Login JWT", saved.Title);
        Assert.Equal(TaskPriority.High, saved.Priority);
        Assert.Equal(TaskItemStatus.InProgress, saved.Status);
        Assert.Equal(user.Id, saved.AssignedToId);

        var unassigned = await service.AssignAsync(task.Id, new AssignTaskRequest(null));
        Assert.Null(unassigned.AssignedToId);

        await service.DeleteAsync(task.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(task.Id));
    }

    [Fact]
    public async Task Tarefa_em_projeto_inexistente_retorna_not_found()
    {
        await using var context = db.CreateContext();
        var service = new TaskService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateTaskRequest("Login", null, TaskPriority.Low, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListByProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Regras_de_dominio_e_usuario_inexistente_sao_respeitadas()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await new ProjectService(context).CreateAsync(user.Id, new CreateProjectRequest("Website", null));
        var service = new TaskService(context);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        await Assert.ThrowsAsync<DomainException>(() =>
            service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.Done)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignAsync(task.Id, new AssignTaskRequest(Guid.NewGuid())));
    }
}
