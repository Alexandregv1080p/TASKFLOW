using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Application;

[Collection(DatabaseCollection.Name)]
public class ServiceTests(DatabaseFixture db)
{
    private record FakeCurrentUser(Guid UserId) : ICurrentUser;

    private static ProjectService Projects(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<ProjectService>.Instance);

    private static TaskService Tasks(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<TaskService>.Instance);

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
        var service = Projects(context,user);

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
        var project = await Projects(context,owner).CreateAsync(new CreateProjectRequest("Privado", null));
        var task = await Tasks(context,owner).CreateAsync(project.Id, new CreateTaskRequest("Segredo", null, TaskPriority.Low, null));

        var projects = Projects(context,intruder);
        var tasks = Tasks(context,intruder);

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
        var project = await Projects(context,user).CreateAsync(new CreateProjectRequest("Website", null));
        var service = Tasks(context,user);

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
    public async Task Historico_registra_cada_alteracao_com_valores_e_autor()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, user).CreateAsync(new CreateProjectRequest("Website", null));
        var service = Tasks(context, user);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        await service.UpdateAsync(task.Id, new UpdateTaskRequest("Login JWT", "Detalhes", TaskPriority.High, new DateTime(2026, 10, 5)));
        await service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        await service.AssignAsync(task.Id, new AssignTaskRequest(user.UserId));

        var history = await service.HistoryAsync(task.Id);

        Assert.All(history, e => Assert.Equal(user.UserId, e.UserId));
        Assert.Equal(
            [
                TaskEventType.Created, TaskEventType.TitleChanged, TaskEventType.DescriptionChanged,
                TaskEventType.PriorityChanged, TaskEventType.DueDateChanged, TaskEventType.StatusChanged,
                TaskEventType.AssigneeChanged
            ],
            history.Select(e => e.Type).Order());

        var title = history.Single(e => e.Type == TaskEventType.TitleChanged);
        Assert.Equal(("Login", "Login JWT"), (title.OldValue, title.NewValue));

        var status = history.Single(e => e.Type == TaskEventType.StatusChanged);
        Assert.Equal(("Todo", "InProgress"), (status.OldValue, status.NewValue));

        var dueDate = history.Single(e => e.Type == TaskEventType.DueDateChanged);
        Assert.Equal((null, "2026-10-05"), (dueDate.OldValue, dueDate.NewValue));

        Assert.Equal(user.UserId.ToString(), history.Single(e => e.Type == TaskEventType.AssigneeChanged).NewValue);
    }

    [Fact]
    public async Task Historico_nao_registra_quando_nada_muda_e_e_restrito_ao_dono()
    {
        var owner = await SeedUserAsync();
        var intruder = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Website", null));
        var service = Tasks(context, owner);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        // Mesmos valores, mesmo status e continuar sem responsável: nenhum evento novo.
        await service.UpdateAsync(task.Id, new UpdateTaskRequest("  Login  ", null, TaskPriority.Low, null));
        await service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.Todo));
        await service.AssignAsync(task.Id, new AssignTaskRequest(null));

        Assert.Equal(TaskEventType.Created, Assert.Single(await service.HistoryAsync(task.Id)).Type);
        await Assert.ThrowsAsync<NotFoundException>(() => Tasks(context, intruder).HistoryAsync(task.Id));
    }

    private static ProjectMemberService Members(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<ProjectMemberService>.Instance);

    private static async Task<string> EmailOfAsync(TaskFlowDbContext context, ICurrentUser user) =>
        (await context.Users.FindAsync(user.UserId))!.Email;

    [Fact]
    public async Task Membro_trabalha_nas_tarefas_mas_nao_administra_o_projeto()
    {
        var owner = await SeedUserAsync();
        var member = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Compartilhado", null));
        var task = await Tasks(context, owner).CreateAsync(project.Id, new CreateTaskRequest("Do dono", null, TaskPriority.Low, null));

        // Antes de entrar: sem acesso.
        await Assert.ThrowsAsync<NotFoundException>(() => Projects(context, member).GetAsync(project.Id));

        var added = await Members(context, owner).AddAsync(project.Id, new AddMemberRequest((await EmailOfAsync(context, member)).ToUpperInvariant()));
        Assert.Equal((member.UserId, false), (added.Id, added.IsOwner));

        // Depois: enxerga o projeto e trabalha nas tarefas.
        var memberTasks = Tasks(context, member);
        Assert.Contains(await Projects(context, member).ListAsync(), p => p.Id == project.Id && p.OwnerId == owner.UserId);
        await memberTasks.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        var created = await memberTasks.CreateAsync(project.Id, new CreateTaskRequest("Do membro", null, TaskPriority.High, null));
        await memberTasks.AssignAsync(created.Id, new AssignTaskRequest(owner.UserId));
        await memberTasks.AddCommentAsync(task.Id, new AddCommentRequest("Vi aqui."));
        Assert.Equal(2, created.Number);

        // Mas administrar o projeto é só do dono: 403.
        await Assert.ThrowsAsync<ForbiddenException>(() => Projects(context, member).UpdateAsync(project.Id, new UpdateProjectRequest("Meu", null)));
        await Assert.ThrowsAsync<ForbiddenException>(() => Projects(context, member).DeleteAsync(project.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => Members(context, member).AddAsync(project.Id, new AddMemberRequest("x@example.com")));

        var people = await Members(context, member).ListAsync(project.Id);
        Assert.Equal([(owner.UserId, true), (member.UserId, false)], people.Select(p => (p.Id, p.IsOwner)));
    }

    [Fact]
    public async Task Regras_de_gestao_de_membros()
    {
        var owner = await SeedUserAsync();
        var member = await SeedUserAsync();
        var other = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Compartilhado", null));
        var members = Members(context, owner);
        var memberEmail = await EmailOfAsync(context, member);

        await members.AddAsync(project.Id, new AddMemberRequest(memberEmail));
        await members.AddAsync(project.Id, new AddMemberRequest(await EmailOfAsync(context, other)));

        // E-mail sem conta, quem já está no projeto e o próprio dono.
        await Assert.ThrowsAsync<NotFoundException>(() => members.AddAsync(project.Id, new AddMemberRequest("ninguem@example.com")));
        await Assert.ThrowsAsync<ConflictException>(() => members.AddAsync(project.Id, new AddMemberRequest(memberEmail)));
        var ownerEmail = await EmailOfAsync(context, owner);
        await Assert.ThrowsAsync<ConflictException>(() => members.AddAsync(project.Id, new AddMemberRequest(ownerEmail)));

        // O dono não pode ser removido; membro não remove outro membro.
        await Assert.ThrowsAsync<DomainException>(() => members.RemoveAsync(project.Id, owner.UserId));
        await Assert.ThrowsAsync<ForbiddenException>(() => Members(context, member).RemoveAsync(project.Id, other.UserId));

        // Só quem está no projeto pode ser responsável.
        var tasks = Tasks(context, owner);
        var task = await tasks.CreateAsync(project.Id, new CreateTaskRequest("Tarefa", null, TaskPriority.Low, null));
        var outsider = await SeedUserAsync();
        await Assert.ThrowsAsync<NotFoundException>(() => tasks.AssignAsync(task.Id, new AssignTaskRequest(outsider.UserId)));
        await tasks.AssignAsync(task.Id, new AssignTaskRequest(member.UserId));

        // Membro sai por conta própria: perde o acesso e a tarefa dele fica sem responsável, com histórico.
        await Members(context, member).RemoveAsync(project.Id, member.UserId);

        await Assert.ThrowsAsync<NotFoundException>(() => Projects(context, member).GetAsync(project.Id));
        Assert.Null((await tasks.GetAsync(task.Id)).AssignedToId);

        var last = (await tasks.HistoryAsync(task.Id)).First();
        Assert.Equal((TaskEventType.AssigneeChanged, member.UserId.ToString(), null), (last.Type, last.OldValue, last.NewValue));

        // Dono remove o outro membro.
        await members.RemoveAsync(project.Id, other.UserId);
        Assert.Equal(owner.UserId, Assert.Single(await members.ListAsync(project.Id)).Id);
    }

    [Fact]
    public async Task Comentarios_adicionar_listar_e_excluir()
    {
        var owner = await SeedUserAsync();
        var intruder = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Website", null));
        var service = Tasks(context, owner);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        var first = await service.AddCommentAsync(task.Id, new AddCommentRequest("  Primeiro comentário  "));
        var second = await service.AddCommentAsync(task.Id, new AddCommentRequest("Segundo"));

        Assert.Equal("Primeiro comentário", first.Body);
        Assert.Equal(owner.UserId, first.AuthorId);
        Assert.Equal([second.Id, first.Id], (await service.ListCommentsAsync(task.Id)).Select(c => c.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.AddCommentAsync(task.Id, new AddCommentRequest("   ")));
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AddCommentAsync(task.Id, new AddCommentRequest(new string('a', TaskComment.BodyMaxLength + 1))));

        // Quem não tem acesso ao projeto não lê, não comenta e não exclui.
        var outsider = Tasks(context, intruder);
        await Assert.ThrowsAsync<NotFoundException>(() => outsider.ListCommentsAsync(task.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => outsider.AddCommentAsync(task.Id, new AddCommentRequest("oi")));
        await Assert.ThrowsAsync<NotFoundException>(() => outsider.DeleteCommentAsync(first.Id));

        await service.DeleteCommentAsync(first.Id);
        Assert.Equal(second.Id, Assert.Single(await service.ListCommentsAsync(task.Id)).Id);
    }

    [Fact]
    public async Task Tarefas_recebem_numero_sequencial_por_projeto()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var projects = Projects(context,user);
        var a = await projects.CreateAsync(new CreateProjectRequest("A", null));
        var b = await projects.CreateAsync(new CreateProjectRequest("B", null));
        var service = Tasks(context,user);

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
        var service = Tasks(context,user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateTaskRequest("Login", null, TaskPriority.Low, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListByProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Regras_de_dominio_e_usuario_inexistente_sao_respeitadas()
    {
        var user = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context,user).CreateAsync(new CreateProjectRequest("Website", null));
        var service = Tasks(context,user);
        var task = await service.CreateAsync(project.Id, new CreateTaskRequest("Login", null, TaskPriority.Low, null));

        await Assert.ThrowsAsync<DomainException>(() =>
            service.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.Done)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignAsync(task.Id, new AssignTaskRequest(Guid.NewGuid())));
    }
}
