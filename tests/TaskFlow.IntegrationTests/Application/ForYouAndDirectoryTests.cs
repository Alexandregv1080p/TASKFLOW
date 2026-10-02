using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.ForYou;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Application;

[Collection(DatabaseCollection.Name)]
public class ForYouAndDirectoryTests(DatabaseFixture db)
{
    private record FakeCurrentUser(Guid UserId) : ICurrentUser;

    private static ProjectService Projects(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<ProjectService>.Instance);

    private static TaskService Tasks(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<TaskService>.Instance);

    private static ForYouService ForYou(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, Projects(context, user));

    private async Task<(ICurrentUser user, string email)> SeedUserAsync(string name = "Ana")
    {
        var user = new User(name, $"{Guid.NewGuid():N}@example.com", "hash");

        await using var context = db.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (new FakeCurrentUser(user.Id), user.Email);
    }

    private static CreateTaskRequest NewTask(string title) => new(title, null, TaskPriority.Medium, null);

    // ---------- Diretório de projetos ----------

    [Fact]
    public async Task Diretorio_separa_todos_meus_e_arquivados()
    {
        var (me, myEmail) = await SeedUserAsync("Eu");
        var (other, _) = await SeedUserAsync("Outra");

        await using var context = db.CreateContext();
        var mine = await Projects(context, me).CreateAsync(new CreateProjectRequest("Meu ativo", null));
        var archived = await Projects(context, me).CreateAsync(new CreateProjectRequest("Meu arquivado", null));
        var shared = await Projects(context, other).CreateAsync(new CreateProjectRequest("Compartilhado comigo", null));
        await new ProjectMemberService(context, other, NullLogger<ProjectMemberService>.Instance)
            .AddAsync(shared.Id, new AddMemberRequest(myEmail));
        await Projects(context, me).ArchiveAsync(archived.Id);

        var service = Projects(context, me);

        Assert.Equal([shared.Id, mine.Id], (await service.ListAsync(ProjectScope.All)).Select(p => p.Id));
        Assert.Equal(mine.Id, Assert.Single(await service.ListAsync(ProjectScope.Mine)).Id);

        var archivedItem = Assert.Single(await service.ListAsync(ProjectScope.Archived));
        Assert.Equal((archived.Id, true), (archivedItem.Id, archivedItem.Archived));

        // Cada linha traz o nome do dono e o status, sem consulta extra do frontend.
        var sharedItem = (await service.ListAsync()).Single(p => p.Id == shared.Id);
        Assert.Equal(("Outra", ProjectStatus.OnTrack), (sharedItem.OwnerName, sharedItem.Status));
    }

    [Fact]
    public async Task Diretorio_busca_por_trecho_do_nome_sem_diferenciar_maiusculas()
    {
        var (me, _) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var service = Projects(context, me);
        await service.CreateAsync(new CreateProjectRequest("Portal do Cliente", null));
        await service.CreateAsync(new CreateProjectRequest("App Mobile", null));

        Assert.Equal("Portal do Cliente", Assert.Single(await service.ListAsync(search: "  PORTAL ")).Name);
        Assert.Equal("App Mobile", Assert.Single(await service.ListAsync(search: "mob")).Name);
        Assert.Empty(await service.ListAsync(search: "inexistente"));
        Assert.Equal(2, (await service.ListAsync(search: "   ")).Count);
    }

    [Fact]
    public async Task Status_e_arquivamento_sao_acoes_do_dono()
    {
        var (owner, _) = await SeedUserAsync();
        var (member, memberEmail) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Time", null));
        await new ProjectMemberService(context, owner, NullLogger<ProjectMemberService>.Instance)
            .AddAsync(project.Id, new AddMemberRequest(memberEmail));

        var updated = await Projects(context, owner).ChangeStatusAsync(project.Id, new ChangeProjectStatusRequest(ProjectStatus.AtRisk));
        Assert.Equal(ProjectStatus.AtRisk, updated.Status);
        Assert.True(updated.StatusUpdatedAt >= project.StatusUpdatedAt);

        var asMember = Projects(context, member);
        await Assert.ThrowsAsync<ForbiddenException>(() => asMember.ChangeStatusAsync(project.Id, new ChangeProjectStatusRequest(ProjectStatus.Paused)));
        await Assert.ThrowsAsync<ForbiddenException>(() => asMember.ArchiveAsync(project.Id));
    }

    [Fact]
    public async Task Projeto_arquivado_fica_somente_leitura_e_volta_ao_ser_restaurado()
    {
        var (me, _) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var projects = Projects(context, me);
        var tasks = Tasks(context, me);
        var project = await projects.CreateAsync(new CreateProjectRequest("Legado", null));
        var task = await tasks.CreateAsync(project.Id, NewTask("Antiga"));

        Assert.NotNull((await projects.ArchiveAsync(project.Id)).ArchivedAt);

        // Continua legível...
        Assert.Single(await tasks.ListByProjectAsync(project.Id));
        Assert.Equal("Antiga", (await tasks.GetAsync(task.Id)).Title);

        // ...mas nada pode ser alterado.
        await Assert.ThrowsAsync<DomainException>(() => tasks.CreateAsync(project.Id, NewTask("Nova")));
        await Assert.ThrowsAsync<DomainException>(() => tasks.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress)));
        await Assert.ThrowsAsync<DomainException>(() => tasks.AddCommentAsync(task.Id, new AddCommentRequest("oi")));
        await Assert.ThrowsAsync<DomainException>(() => tasks.DeleteAsync(task.Id));
        await Assert.ThrowsAsync<DomainException>(() => projects.UpdateAsync(project.Id, new UpdateProjectRequest("Outro", null)));

        Assert.Null((await projects.RestoreAsync(project.Id)).ArchivedAt);
        await tasks.ChangeStatusAsync(task.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
    }

    // ---------- Para você ----------

    [Fact]
    public async Task Estrela_e_visto_sao_pessoais_e_idempotentes()
    {
        var (owner, _) = await SeedUserAsync();
        var (member, memberEmail) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Time", null));
        await new ProjectMemberService(context, owner, NullLogger<ProjectMemberService>.Instance)
            .AddAsync(project.Id, new AddMemberRequest(memberEmail));
        var task = await Tasks(context, owner).CreateAsync(project.Id, NewTask("Com estrela"));

        var tasks = Tasks(context, owner);
        Assert.True((await tasks.SetStarAsync(task.Id, true)).Starred);
        Assert.True((await tasks.SetStarAsync(task.Id, true)).Starred);   // marcar de novo não é erro
        await tasks.MarkViewedAsync(task.Id);
        await tasks.MarkViewedAsync(task.Id);                             // abrir de novo só atualiza a data

        // A estrela aparece nas respostas de tarefa de quem marcou, e só dele.
        Assert.True(Assert.Single(await tasks.ListByProjectAsync(project.Id)).Starred);
        Assert.True((await tasks.GetAsync(task.Id)).Starred);
        Assert.False(Assert.Single(await Tasks(context, member).ListByProjectAsync(project.Id)).Starred);

        Assert.Equal(task.Id, Assert.Single((await ForYou(context, owner).GetAsync(ForYouFilter.Starred)).Items).TaskId);
        Assert.Equal(task.Id, Assert.Single((await ForYou(context, owner).GetAsync(ForYouFilter.Viewed)).Items).TaskId);
        Assert.Empty((await ForYou(context, member).GetAsync(ForYouFilter.Starred)).Items);
        Assert.Empty((await ForYou(context, member).GetAsync(ForYouFilter.Viewed)).Items);

        Assert.False((await tasks.SetStarAsync(task.Id, false)).Starred);
        Assert.False((await tasks.SetStarAsync(task.Id, false)).Starred);  // desmarcar de novo não é erro
        Assert.Empty((await ForYou(context, owner).GetAsync(ForYouFilter.Starred)).Items);
    }

    [Fact]
    public async Task Abas_atribuido_trabalhado_e_recomendado()
    {
        var (me, myEmail) = await SeedUserAsync("Eu");
        var (colleague, _) = await SeedUserAsync("Colega");

        await using var context = db.CreateContext();
        var project = await Projects(context, colleague).CreateAsync(new CreateProjectRequest("Time", null));
        await new ProjectMemberService(context, colleague, NullLogger<ProjectMemberService>.Instance)
            .AddAsync(project.Id, new AddMemberRequest(myEmail));

        var theirs = Tasks(context, colleague);
        var assignedOpen = await theirs.CreateAsync(project.Id, NewTask("Atribuída a mim, aberta"));
        var assignedDone = await theirs.CreateAsync(project.Id, NewTask("Atribuída a mim, concluída"));
        var untouched = await theirs.CreateAsync(project.Id, NewTask("Não é comigo"));
        var commented = await theirs.CreateAsync(project.Id, NewTask("Só comentei"));
        await theirs.AssignAsync(assignedOpen.Id, new AssignTaskRequest(me.UserId));
        await theirs.AssignAsync(assignedDone.Id, new AssignTaskRequest(me.UserId));

        var mine = Tasks(context, me);
        await mine.ChangeStatusAsync(assignedDone.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        await mine.ChangeStatusAsync(assignedDone.Id, new ChangeTaskStatusRequest(TaskItemStatus.Done));
        await mine.AddCommentAsync(commented.Id, new AddCommentRequest("Olhei."));

        var forYou = ForYou(context, me);

        // Atribuído a mim: só as abertas; o contador acompanha.
        var assigned = await forYou.GetAsync(ForYouFilter.Assigned);
        var assignedItem = Assert.Single(assigned.Items);
        Assert.Equal((assignedOpen.Id, "Time", "Eu"), (assignedItem.TaskId, assignedItem.ProjectName, assignedItem.AssigneeName));
        Assert.Equal(1, assigned.AssignedCount);

        // Trabalhado: o que eu alterei ou comentei, mesmo concluído; mais recente primeiro.
        var worked = await forYou.GetAsync(ForYouFilter.Worked);
        Assert.Equal([commented.Id, assignedDone.Id], worked.Items.Select(i => i.TaskId));

        // Recomendado: abertas atribuídas a mim + abertas em que trabalhei (a concluída sai).
        var recommended = await forYou.GetAsync(ForYouFilter.Recommended);
        Assert.Equal(new[] { assignedOpen.Id, commented.Id }.Order(), recommended.Items.Select(i => i.TaskId).Order());
        Assert.DoesNotContain(recommended.Items, i => i.TaskId == untouched.Id);

        // Espaços recomendados: os projetos a que tenho acesso.
        Assert.Equal(project.Id, Assert.Single(recommended.RecommendedProjects).Id);
    }

    [Fact]
    public async Task Para_voce_ignora_projetos_arquivados_e_de_outros()
    {
        var (me, _) = await SeedUserAsync();
        var (stranger, _) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var archived = await Projects(context, me).CreateAsync(new CreateProjectRequest("Arquivado", null));
        var task = await Tasks(context, me).CreateAsync(archived.Id, NewTask("Velha"));
        await Tasks(context, me).AssignAsync(task.Id, new AssignTaskRequest(me.UserId));
        await Tasks(context, me).SetStarAsync(task.Id, true);
        await Projects(context, me).ArchiveAsync(archived.Id);

        var foreign = await Projects(context, stranger).CreateAsync(new CreateProjectRequest("De outro", null));
        await Tasks(context, stranger).CreateAsync(foreign.Id, NewTask("Não é minha"));

        foreach (var filter in Enum.GetValues<ForYouFilter>())
        {
            var response = await ForYou(context, me).GetAsync(filter);

            Assert.Empty(response.Items);
            Assert.Empty(response.RecommendedProjects);
            Assert.Equal(0, response.AssignedCount);
        }
    }
}
