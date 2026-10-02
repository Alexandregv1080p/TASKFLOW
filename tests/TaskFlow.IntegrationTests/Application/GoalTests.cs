using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Goals;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Api;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Application;

[Collection(DatabaseCollection.Name)]
public class GoalTests(DatabaseFixture db)
{
    private record FakeCurrentUser(Guid UserId) : ICurrentUser;

    private static GoalService Goals(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<GoalService>.Instance);

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
    public async Task Progresso_da_meta_vem_das_tarefas_dos_projetos_ligados()
    {
        var me = await SeedUserAsync();

        await using var context = db.CreateContext();
        var site = await Projects(context, me).CreateAsync(new CreateProjectRequest("Site", null));
        var app = await Projects(context, me).CreateAsync(new CreateProjectRequest("App", null));
        var tasks = Tasks(context, me);
        var done = await tasks.CreateAsync(site.Id, new CreateTaskRequest("Feita", null, TaskPriority.Low, null));
        await tasks.CreateAsync(site.Id, new CreateTaskRequest("Aberta", null, TaskPriority.Low, null));
        await tasks.CreateAsync(app.Id, new CreateTaskRequest("Do app", null, TaskPriority.Low, null));
        await tasks.ChangeStatusAsync(done.Id, new ChangeTaskStatusRequest(TaskItemStatus.InProgress));
        await tasks.ChangeStatusAsync(done.Id, new ChangeTaskStatusRequest(TaskItemStatus.Done));

        var goals = Goals(context, me);
        var goal = await goals.CreateAsync(new GoalRequest(
            "  Lançar em novembro  ", "Site e app no ar", new DateTime(2026, 11, 30, 15, 0, 0), ProjectStatus.AtRisk, [site.Id, app.Id, site.Id]));

        Assert.Equal("Lançar em novembro", goal.Name);
        Assert.Equal(new DateTime(2026, 11, 30), goal.TargetDate);       // só a data, sem hora
        Assert.Equal(ProjectStatus.AtRisk, goal.Status);
        Assert.Equal(["App", "Site"], goal.Projects.Select(p => p.Name)); // sem repetir o projeto enviado duas vezes
        Assert.Equal((3, 1), (goal.TaskCount, goal.DoneCount));

        // Atualizar troca o conjunto de projetos ligados.
        var updated = await goals.UpdateAsync(goal.Id, new GoalRequest("Lançar o site", null, null, ProjectStatus.OnTrack, [site.Id]));
        Assert.Equal(("Lançar o site", null, null), (updated.Name, updated.Description, updated.TargetDate));
        Assert.Equal((2, 1), (updated.TaskCount, updated.DoneCount));
        Assert.Equal("Site", Assert.Single(updated.Projects).Name);

        // Sem projetos ligados a meta continua existindo, com progresso zerado.
        var unlinked = await goals.UpdateAsync(goal.Id, new GoalRequest("Lançar o site", null, null, ProjectStatus.OnTrack, null));
        Assert.Empty(unlinked.Projects);
        Assert.Equal(0, unlinked.TaskCount);

        Assert.Equal(goal.Id, Assert.Single(await goals.ListAsync()).Id);

        await goals.DeleteAsync(goal.Id);
        Assert.Empty(await goals.ListAsync());
    }

    [Fact]
    public async Task Metas_sao_pessoais_e_so_ligam_projetos_acessiveis()
    {
        var me = await SeedUserAsync();
        var other = await SeedUserAsync();

        await using var context = db.CreateContext();
        var mine = await Projects(context, me).CreateAsync(new CreateProjectRequest("Meu", null));
        var theirs = await Projects(context, other).CreateAsync(new CreateProjectRequest("Deles", null));
        var goal = await Goals(context, me).CreateAsync(new GoalRequest("Minha meta", null, null, ProjectStatus.OnTrack, [mine.Id]));

        // Outro usuário não vê, não altera e não exclui.
        var asOther = Goals(context, other);
        Assert.Empty(await asOther.ListAsync());
        await Assert.ThrowsAsync<NotFoundException>(() => asOther.UpdateAsync(goal.Id, new GoalRequest("Roubada", null, null, ProjectStatus.OnTrack, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => asOther.DeleteAsync(goal.Id));

        // Não dá para ligar projeto a que não tenho acesso, nem projeto inexistente.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Goals(context, me).CreateAsync(new GoalRequest("Outra", null, null, ProjectStatus.OnTrack, [theirs.Id])));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Goals(context, me).UpdateAsync(goal.Id, new GoalRequest("Minha meta", null, null, ProjectStatus.OnTrack, [Guid.NewGuid()])));

        // Validação de domínio.
        await Assert.ThrowsAsync<DomainException>(() =>
            Goals(context, me).CreateAsync(new GoalRequest("  ", null, null, ProjectStatus.OnTrack, null)));
        await Assert.ThrowsAsync<DomainException>(() =>
            Goals(context, me).CreateAsync(new GoalRequest("Ok", null, null, (ProjectStatus)99, null)));
    }

    [Fact]
    public async Task Excluir_o_projeto_desfaz_a_ligacao_mas_mantem_a_meta()
    {
        var me = await SeedUserAsync();

        await using var context = db.CreateContext();
        var project = await Projects(context, me).CreateAsync(new CreateProjectRequest("Temporário", null));
        var goal = await Goals(context, me).CreateAsync(new GoalRequest("Meta", null, null, ProjectStatus.OnTrack, [project.Id]));

        await Projects(context, me).DeleteAsync(project.Id);

        var remaining = Assert.Single(await Goals(context, me).ListAsync());
        Assert.Equal(goal.Id, remaining.Id);
        Assert.Empty(remaining.Projects);
    }

    [Fact]
    public async Task Metas_pela_api()
    {
        var client = await db.CreateAuthenticatedClientAsync();
        var project = await (await client.PostAsJsonAsync("/api/projects", new { name = "Site" })).Content.ReadFromJsonAsync<JsonElement>();
        var projectId = project.GetProperty("id").GetString();

        var created = await client.PostAsJsonAsync("/api/goals",
            new { name = "Lançar", targetDate = "2026-12-15", status = "AtRisk", projectIds = new[] { projectId } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var goal = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = goal.GetProperty("id").GetString();
        Assert.Equal("AtRisk", goal.GetProperty("status").GetString());
        Assert.StartsWith("2026-12-15", goal.GetProperty("targetDate").GetString());
        Assert.Equal("Site", goal.GetProperty("projects")[0].GetProperty("name").GetString());

        await ApiTests.AssertErrorAsync(await client.PostAsJsonAsync("/api/goals", new { name = "", status = "OnTrack" }), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(await client.PostAsJsonAsync("/api/goals", new { name = "X", status = "Inexistente" }), HttpStatusCode.BadRequest);

        var updated = await client.PutAsJsonAsync($"/api/goals/{id}", new { name = "Lançado", status = "Completed" });
        Assert.Equal("Completed", (await updated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/goals")).GetArrayLength());

        // Outro usuário recebe 404; sem token, 401.
        var other = await db.CreateAuthenticatedClientAsync("Outro");
        await ApiTests.AssertErrorAsync(await other.DeleteAsync($"/api/goals/{id}"), HttpStatusCode.NotFound);
        await ApiTests.AssertErrorAsync(await db.Api.CreateClient().GetAsync("/api/goals"), HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/goals/{id}")).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/goals")).GetArrayLength());
    }
}
