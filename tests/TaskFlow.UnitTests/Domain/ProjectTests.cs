using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class ProjectTests
{
    [Fact]
    public void Deve_criar_projeto_com_dados_validos()
    {
        var ownerId = Guid.NewGuid();

        var project = new Project("  Website  ", "Portfólio", ownerId);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("Website", project.Name);
        Assert.Equal(ownerId, project.OwnerId);
        Assert.True(DateTime.UtcNow - project.CreatedAt < TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deve_rejeitar_nome_vazio(string? name)
    {
        Assert.Throws<DomainException>(() => new Project(name!, null, Guid.NewGuid()));
    }

    [Fact]
    public void Deve_rejeitar_nome_longo_demais()
    {
        var name = new string('a', Project.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new Project(name, null, Guid.NewGuid()));
    }

    [Fact]
    public void Deve_rejeitar_projeto_sem_dono()
    {
        Assert.Throws<DomainException>(() => new Project("Website", null, Guid.Empty));
    }

    [Fact]
    public void Update_deve_alterar_dados_e_tratar_descricao_vazia_como_nula()
    {
        var project = new Project("Website", "Antiga", Guid.NewGuid());

        project.Update("Site novo", "  ");

        Assert.Equal("Site novo", project.Name);
        Assert.Null(project.Description);
    }
}
