using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
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
    public void Novo_projeto_comeca_no_prazo_e_ativo()
    {
        var project = new Project("Website", null, Guid.NewGuid());

        Assert.Equal(ProjectStatus.OnTrack, project.Status);
        Assert.False(project.IsArchived);
    }

    [Fact]
    public void ChangeStatus_atualiza_o_status_e_a_data_da_atualizacao()
    {
        var project = new Project("Website", null, Guid.NewGuid());
        var before = project.StatusUpdatedAt;

        project.ChangeStatus(ProjectStatus.AtRisk);

        Assert.Equal(ProjectStatus.AtRisk, project.Status);
        Assert.True(project.StatusUpdatedAt >= before);
        Assert.Throws<DomainException>(() => project.ChangeStatus((ProjectStatus)99));
    }

    [Fact]
    public void Projeto_arquivado_e_somente_leitura_ate_ser_restaurado()
    {
        var project = new Project("Website", null, Guid.NewGuid());

        project.Archive();

        Assert.True(project.IsArchived);
        Assert.Throws<DomainException>(() => project.Update("Outro", null));
        Assert.Throws<DomainException>(() => project.ChangeStatus(ProjectStatus.Paused));

        project.Restore();

        Assert.False(project.IsArchived);
        project.Update("Outro", null);
        Assert.Equal("Outro", project.Name);
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
