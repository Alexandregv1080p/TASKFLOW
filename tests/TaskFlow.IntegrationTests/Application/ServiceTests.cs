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
    private record FakeCurrentUser(Guid UserId) : ICurrentUser;

    private async Task<ICurrentUser> SeedUserAsync()
    {
        var user = new User("Ana", $"{Guid.NewGuid():N}@example.com", "hash");

        await using var context = db.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return new FakeCurrentUser(user.Id);
    }

    [Fact]
    public async Task Ciclo_completo_de_projeto()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var service = new ProjectService(context, user);

        var created = await service.CreateAsync(new CreateProjectRequest("Website", null));
        var updated = await service.UpdateAsync(created.Id, new UpdateProjectRequest("Site novo", "Descrição"));

        Assert.Equal(user.UserId, created.OwnerId);
        Assert.Equal("Site novo", updated.Name);
        Assert.Contains(await service.ListAsync(), p => p.Id == created.Id);

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(created.Id));
    }

    [Fact]
    public async Task Usuario_nao_acessa_projeto_nem_tarefa_de_outro()
    {
        var owner = await SeedUserAsync();
        var intruder = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await new ProjectService(context, owner).CreateAsync(new CreateProjectRequest("Privado", null));
        var task = await new TaskService(context, owner).CreateAsync(project.Id, new CreateTaskRequest("Segredo", null, TaskPriority.Low, null));

        var projects = new ProjectService(context, intruder);
        var tasks = new TaskService(context, intruder);

        Assert.DoesNotContain(await projects.ListAsync(), p => p.Id == project.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => projects.GetAsync(project.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => projects.DeleteAsync(project.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => tasks.ListByProjectAsync(project.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => tasks.GetAsync(task.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            tasks.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress)));
    }

    [Fact]
    public async Task Ciclo_completo_de_tarefa()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await new ProjectService(context, user).CreateAsync(new CreateProjectRequest("Website", null));
        var service = new TaskService(context, user);

        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));
        await service.UpdateAsync(task.Id, new UpdateTaskRequest("Login JWT", "Detalhes", TaskPriority.High, new DateTime(2026, 10, 5)));
        await service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        await service.AssignAsync(task.Id, new AssignTaskRequest(user.UserId));

        var saved = Assert.Single(await service.ListByProjectAsync(project.Id));
        Assert.Equal("Login JWT", saved.Title);
        Assert.Equal(TaskPriority.High, saved.Priority);
        Assert.Equal(TaskItemStatus.InProgress, saved.Status);
        Assert.Equal(user.UserId, saved.AssignedToId);
        Assert.Equal(user.UserId, saved.CreatedById);

        var unassigned = await service.AssignAsync(task.Id, new AssignTaskRequest(null));
        Assert.Null(unassigned.AssignedToId);

        await service.DeleteAsync(task.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(task.Id));
    }

    [Fact]
    public async Task Tarefas_recebem_numero_sequencial_por_projeto()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var projects = new ProjectService(context, user);
        var a = await projects.CreateAsync(new CreateProjectRequest("A", null));
        var b = await projects.CreateAsync(new CreateProjectRequest("B", null));
        var service = new TaskService(context, user);

        var first = await service.CreateAsync(a.Id, new CreateTaskRequest("1", null, TaskPriority.Low, null));
        var second = await service.CreateAsync(a.Id, new CreateTaskRequest("2", null, TaskPriority.Low, null));
        var otherProject = await service.CreateAsync(b.Id, new CreateTaskRequest("1", null, TaskPriority.Low, null));

        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.Equal(1, otherProject.Number);
    }

    [Fact]
    public async Task Tarefa_em_projeto_inexistente_retorna_not_found()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var service = new TaskService(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateTaskRequest("Login", null, TaskPriority.Low, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListByProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Regras_de_dominio_e_usuario_inexistente_sao_respeitadas()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await new ProjectService(context, user).CreateAsync(new CreateProjectRequest("Website", null));
        var service = new TaskService(context, user);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        await Assert.ThrowsAsync<DomainException>(() =>
            service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.Done)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignAsync(task.Id, new AssignTaskRequest(Guid.NewGuid())));
    }
}
