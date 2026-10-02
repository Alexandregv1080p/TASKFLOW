using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Spaces;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Api;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Application;

[Collection(DatabaseCollection.Name)]
public class SpaceTests(DatabaseFixture db)
{
    private record FakeCurrentUser(Guid UserId) : ICurrentUser;

    private static ProjectService Projects(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, NullLogger<ProjectService>.Instance);

    private static SpaceService Spaces(TaskFlowDbContext context, ICurrentUser user) =>
        new(context, user, Projects(context, user), NullLogger<SpaceService>.Instance);

    private async Task<(ICurrentUser user, string email)> SeedUserAsync(string name = "Ana")
    {
        var user = new User(name, $"{Guid.NewGuid():N}@example.com", "hash");

        await using var context = db.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (new FakeCurrentUser(user.Id), user.Email);
    }

    [Fact]
    public void Catalogo_de_modelos_e_consistente()
    {
        var templates = SpaceTemplateCatalog.All;

        Assert.Equal(templates.Count, templates.Select(t => t.Key).Distinct().Count());
        Assert.Empty(SpaceTemplateCatalog.Find(SpaceTemplateCatalog.BlankKey)!.Projects);
        Assert.Contains(templates, t => t.Recommended);

        // Todo texto do catálogo precisa caber nas regras do domínio, senão criar o espaço falharia.
        Assert.All(templates.SelectMany(t => t.Projects), p =>
        {
            Assert.InRange(p.Name.Length, 1, Project.NameMaxLength);
            Assert.InRange(p.Description.Length, 1, Project.DescriptionMaxLength);
            Assert.All(p.Tasks, t => Assert.InRange(t.Title.Length, 1, TaskItem.TitleMaxLength));
        });

        Assert.Same(templates[0], SpaceTemplateCatalog.Find(null));   // sem modelo = em branco
        Assert.Null(SpaceTemplateCatalog.Find("nao-existe"));
    }

    [Fact]
    public async Task Espaco_em_branco_e_a_partir_de_modelo()
    {
        var (me, _) = await SeedUserAsync("Eu");

        await using var context = db.CreateContext();
        var spaces = Spaces(context, me);

        var blank = await spaces.CreateAsync(new CreateSpaceRequest("  Financeiro  ", "Contas e orçamento"));
        Assert.Equal(("Financeiro", "Eu"), (blank.Name, blank.OwnerName));
        Assert.Empty(blank.Projects);

        // Modelo Scrum: cria os projetos e as tarefas iniciais junto com o espaço.
        var scrum = await spaces.CreateAsync(new CreateSpaceRequest("Produto", null, "scrum"));
        Assert.Equal(new[] { "Backlog do produto", "Sprint atual" }, scrum.Projects.Select(p => p.Name).Order());
        Assert.All(scrum.Projects, p =>
        {
            Assert.Equal((scrum.Id, "Produto", me.UserId), (p.SpaceId, p.SpaceName, p.OwnerId));
            Assert.True(p.TaskCount > 0);
        });

        // As tarefas do modelo são tarefas normais: numeradas, com criador e com histórico.
        var backlog = scrum.Projects.Single(p => p.Name == "Backlog do produto");
        var tasks = new TaskService(context, me, NullLogger<TaskService>.Instance);
        var created = await tasks.ListByProjectAsync(backlog.Id);
        Assert.Equal(Enumerable.Range(1, created.Count), created.Select(t => t.Number));
        Assert.All(created, t => Assert.Equal(me.UserId, t.CreatedById));
        Assert.Equal(TaskEventType.Created, Assert.Single(await tasks.HistoryAsync(created[0].Id)).Type);

        await Assert.ThrowsAsync<NotFoundException>(() => spaces.CreateAsync(new CreateSpaceRequest("X", null, "nao-existe")));
        await Assert.ThrowsAsync<DomainException>(() => spaces.CreateAsync(new CreateSpaceRequest("   ", null)));

        var list = await spaces.ListAsync();
        Assert.Equal([scrum.Id, blank.Id], list.Select(s => s.Id));
        Assert.Equal([2, 0], list.Select(s => s.ProjectCount));
        Assert.Equal(scrum.Id, Assert.Single(await spaces.ListAsync(limit: 1)).Id);
    }

    [Fact]
    public async Task Projeto_so_entra_em_espaco_do_proprio_usuario()
    {
        var (me, _) = await SeedUserAsync();
        var (other, _) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var mine = await Spaces(context, me).CreateAsync(new CreateSpaceRequest("Meu espaço", null));
        var theirs = await Spaces(context, other).CreateAsync(new CreateSpaceRequest("Deles", null));
        var projects = Projects(context, me);

        // Criar direto dentro do espaço.
        var inside = await projects.CreateAsync(new CreateProjectRequest("Dentro", null, mine.Id));
        Assert.Equal((mine.Id, "Meu espaço"), (inside.SpaceId, inside.SpaceName));

        // Mover um projeto avulso para o espaço e tirar de volta.
        var loose = await projects.CreateAsync(new CreateProjectRequest("Avulso", null));
        Assert.Null(loose.SpaceId);
        Assert.Equal(mine.Id, (await projects.UpdateAsync(loose.Id, new UpdateProjectRequest("Avulso", null, mine.Id))).SpaceId);
        Assert.Equal(2, (await Spaces(context, me).GetAsync(mine.Id)).Projects.Count);
        Assert.Null((await projects.UpdateAsync(loose.Id, new UpdateProjectRequest("Avulso", null))).SpaceId);

        // Filtro do diretório por espaço.
        Assert.Equal(inside.Id, Assert.Single(await projects.ListAsync(spaceId: mine.Id)).Id);

        // Espaço de outra pessoa (ou inexistente) responde 404, na criação e na mudança.
        await Assert.ThrowsAsync<NotFoundException>(() => projects.CreateAsync(new CreateProjectRequest("X", null, theirs.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => projects.UpdateAsync(loose.Id, new UpdateProjectRequest("Avulso", null, theirs.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => projects.CreateAsync(new CreateProjectRequest("X", null, Guid.NewGuid())));
    }

    [Fact]
    public async Task Quem_participa_de_um_projeto_ve_o_espaco_so_com_o_que_pode_acessar()
    {
        var (owner, _) = await SeedUserAsync("Dona");
        var (member, memberEmail) = await SeedUserAsync("Membro");
        var (stranger, _) = await SeedUserAsync("Estranho");

        await using var context = db.CreateContext();
        var space = await Spaces(context, owner).CreateAsync(new CreateSpaceRequest("Time", null));
        var shared = await Projects(context, owner).CreateAsync(new CreateProjectRequest("Compartilhado", null, space.Id));
        await Projects(context, owner).CreateAsync(new CreateProjectRequest("Só da dona", null, space.Id));
        await new ProjectMemberService(context, owner, NullLogger<ProjectMemberService>.Instance)
            .AddAsync(shared.Id, new AddMemberRequest(memberEmail));

        // O membro vê o espaço, mas só com o projeto dele.
        var asMember = Spaces(context, member);
        var seen = await asMember.GetAsync(space.Id);
        Assert.Equal(shared.Id, Assert.Single(seen.Projects).Id);
        Assert.Equal(1, Assert.Single(await asMember.ListAsync()).ProjectCount);

        // Mas administrar o espaço é só da dona: 403.
        await Assert.ThrowsAsync<ForbiddenException>(() => asMember.UpdateAsync(space.Id, new UpdateSpaceRequest("Meu", null)));
        await Assert.ThrowsAsync<ForbiddenException>(() => asMember.DeleteAsync(space.Id));

        // Quem não participa de nada ali nem sabe que o espaço existe: 404.
        var asStranger = Spaces(context, stranger);
        Assert.Empty(await asStranger.ListAsync());
        await Assert.ThrowsAsync<NotFoundException>(() => asStranger.GetAsync(space.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => asStranger.DeleteAsync(space.Id));

        Assert.Equal(2, (await Spaces(context, owner).GetAsync(space.Id)).Projects.Count);
    }

    [Fact]
    public async Task Editar_e_excluir_o_espaco_mantendo_os_projetos()
    {
        var (me, _) = await SeedUserAsync();

        await using var context = db.CreateContext();
        var spaces = Spaces(context, me);
        var space = await spaces.CreateAsync(new CreateSpaceRequest("Marketing", null, "kanban"));
        var projectId = Assert.Single(space.Projects).Id;

        var updated = await spaces.UpdateAsync(space.Id, new UpdateSpaceRequest("Marketing e Vendas", "Demanda"));
        Assert.Equal(("Marketing e Vendas", "Demanda"), (updated.Name, updated.Description));

        // Projeto arquivado sai da contagem e da lista do espaço.
        await Projects(context, me).ArchiveAsync(projectId);
        Assert.Empty((await spaces.GetAsync(space.Id)).Projects);
        Assert.Equal(0, Assert.Single(await spaces.ListAsync()).ProjectCount);
        await Projects(context, me).RestoreAsync(projectId);

        await spaces.DeleteAsync(space.Id);

        // O espaço some; o projeto e suas tarefas continuam, agora sem espaço.
        Assert.Empty(await spaces.ListAsync());
        context.ChangeTracker.Clear();
        var project = await Projects(context, me).GetAsync(projectId);
        Assert.Equal((null, null), (project.SpaceId, project.SpaceName));
        Assert.NotEmpty(await new TaskService(context, me, NullLogger<TaskService>.Instance).ListByProjectAsync(projectId));
    }

    [Fact]
    public async Task Espacos_pela_api()
    {
        var client = await db.CreateAuthenticatedClientAsync();

        var templates = await client.GetFromJsonAsync<JsonElement>("/api/spaces/templates");
        Assert.Contains(templates.EnumerateArray(), t => t.GetProperty("key").GetString() == "kanban");

        var created = await client.PostAsJsonAsync("/api/spaces", new { name = "Engenharia", templateKey = "kanban" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var space = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = space.GetProperty("id").GetString();
        Assert.Equal("Quadro Kanban", space.GetProperty("projects")[0].GetProperty("name").GetString());

        await ApiTests.AssertErrorAsync(await client.PostAsJsonAsync("/api/spaces", new { name = "" }), HttpStatusCode.BadRequest);
        await ApiTests.AssertErrorAsync(await client.PostAsJsonAsync("/api/spaces", new { name = "X", templateKey = "sumiu" }), HttpStatusCode.NotFound);

        // Criar projeto dentro do espaço e filtrar o diretório por ele.
        var project = await client.PostAsJsonAsync("/api/projects", new { name = "API", spaceId = id });
        Assert.Equal("Engenharia", (await project.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceName").GetString());
        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>($"/api/projects?spaceId={id}")).GetArrayLength());

        // Para você traz os espaços recomendados.
        var forYou = await client.GetFromJsonAsync<JsonElement>("/api/for-you");
        Assert.Equal(id, forYou.GetProperty("recommendedSpaces")[0].GetProperty("id").GetString());

        // Outro usuário: 404 para ver e para excluir; sem token: 401.
        var other = await db.CreateAuthenticatedClientAsync("Outro");
        await ApiTests.AssertErrorAsync(await other.GetAsync($"/api/spaces/{id}"), HttpStatusCode.NotFound);
        await ApiTests.AssertErrorAsync(await other.DeleteAsync($"/api/spaces/{id}"), HttpStatusCode.NotFound);
        await ApiTests.AssertErrorAsync(await db.Api.CreateClient().GetAsync("/api/spaces"), HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/spaces/{id}")).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/spaces")).GetArrayLength());
        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>("/api/projects")).GetArrayLength());   // projetos preservados
    }
}
