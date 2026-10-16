using TaskFlow.Domain.Common;

namespace TaskFlow.UnitTests.Domain;

// Mesmos casos do site e do app Android: a chave da tarefa precisa ser igual nos três.
public class ProjectKeyTests
{
    [Theory]
    [InlineData("Website", "WEB")]
    [InlineData("Mobile App", "MA")]
    [InlineData("Portal do Cliente", "PDC")]
    [InlineData("Revisão anual", "RA")]
    [InlineData("Ação", "ACA")]
    [InlineData("Backlog do produto (v2) extra", "BDP")]
    [InlineData("  !!  ", "TF")]
    public void Sigla_igual_a_do_site(string name, string key) => Assert.Equal(key, ProjectKey.For(name));

    [Fact]
    public void Chave_da_tarefa() => Assert.Equal("PDC-12", ProjectKey.TaskKey("Portal do Cliente", 12));
}
