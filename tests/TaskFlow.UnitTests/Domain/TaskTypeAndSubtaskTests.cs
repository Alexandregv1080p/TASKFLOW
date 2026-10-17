using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class TaskTypeAndSubtaskTests
{
    private static TaskItem NewTask(Guid projectId, int number = 1) =>
        new(projectId, number, Guid.NewGuid(), "Tarefa", null, TaskPriority.Medium, null);

    [Fact]
    public void Tipo_exige_nome_e_cor_hexadecimal()
    {
        var type = new TaskType(Guid.NewGuid(), " Bug ", "#c9372c");

        Assert.Equal("Bug", type.Name);
        Assert.Equal("#C9372C", type.Color);
        Assert.Throws<DomainException>(() => new TaskType(Guid.NewGuid(), "Bug", "vermelho"));
        Assert.Throws<DomainException>(() => new TaskType(Guid.NewGuid(), "Bug", "#FFF"));
        Assert.Throws<DomainException>(() => new TaskType(Guid.NewGuid(), " ", "#FFFFFF"));
    }

    [Fact]
    public void Projeto_novo_ganha_tres_tipos_padrao()
    {
        var projectId = Guid.NewGuid();
        var defaults = TaskType.Defaults(projectId).ToList();

        Assert.Equal(["Desenvolvimento", "Bug", "Integração"], defaults.Select(t => t.Name));
        Assert.All(defaults, t => Assert.Equal(projectId, t.ProjectId));
        // Datas estritamente crescentes: a lista do projeto (ordenada por data) mantém esta ordem.
        Assert.Equal(defaults.Select(t => t.CreatedAt).Order(), defaults.Select(t => t.CreatedAt));
        Assert.Equal(3, defaults.Select(t => t.CreatedAt).Distinct().Count());
    }

    [Fact]
    public void Subtarefa_fica_no_mesmo_projeto_e_tem_um_nivel_so()
    {
        var projectId = Guid.NewGuid();
        var parent = NewTask(projectId, 1);
        var child = NewTask(projectId, 2);

        child.MakeSubtaskOf(parent);
        Assert.Equal(parent.Id, child.ParentId);

        // Subtarefa de subtarefa, de outro projeto ou de si mesma: não.
        Assert.Throws<DomainException>(() => NewTask(projectId, 3).MakeSubtaskOf(child));
        Assert.Throws<DomainException>(() => NewTask(Guid.NewGuid()).MakeSubtaskOf(parent));
        Assert.Throws<DomainException>(() => parent.MakeSubtaskOf(parent));
    }
}
