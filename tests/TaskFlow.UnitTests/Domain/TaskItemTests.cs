using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class TaskItemTests
{
    private static TaskItem NewTask() =>
        new(Guid.NewGuid(), 1, Guid.NewGuid(), "Implementar login", null, TaskPriority.Medium, null);

    [Fact]
    public void Nova_tarefa_comeca_em_Todo_e_sem_responsavel()
    {
        var task = NewTask();

        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Null(task.AssignedToId);
    }

    [Fact]
    public void Deve_rejeitar_titulo_vazio()
    {
        Assert.Throws<DomainException>(() => new TaskItem(Guid.NewGuid(), 1, Guid.NewGuid(), " ", null, TaskPriority.Low, null));
    }

    [Fact]
    public void Deve_exigir_numero_positivo_e_criador()
    {
        Assert.Throws<DomainException>(() => new TaskItem(Guid.NewGuid(), 0, Guid.NewGuid(), "Login", null, TaskPriority.Low, null));
        Assert.Throws<DomainException>(() => new TaskItem(Guid.NewGuid(), 1, Guid.Empty, "Login", null, TaskPriority.Low, null));
    }

    [Fact]
    public void Deve_seguir_o_fluxo_Todo_InProgress_Done()
    {
        var task = NewTask();

        task.ChangeStatus(TaskItemStatus.InProgress);
        task.ChangeStatus(TaskItemStatus.Done);

        Assert.Equal(TaskItemStatus.Done, task.Status);
    }

    [Fact]
    public void Deve_permitir_voltar_um_passo()
    {
        var task = NewTask();
        task.ChangeStatus(TaskItemStatus.InProgress);

        task.ChangeStatus(TaskItemStatus.Todo);

        Assert.Equal(TaskItemStatus.Todo, task.Status);
    }

    [Fact]
    public void Nao_deve_pular_de_Todo_para_Done()
    {
        var task = NewTask();

        Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.Done));
    }

    [Fact]
    public void Deve_rejeitar_status_e_prioridade_invalidos()
    {
        var task = NewTask();

        Assert.Throws<DomainException>(() => task.ChangeStatus((TaskItemStatus)99));
        Assert.Throws<DomainException>(() => task.ChangePriority((TaskPriority)99));
    }

    [Fact]
    public void Deve_atribuir_e_remover_responsavel()
    {
        var task = NewTask();
        var userId = Guid.NewGuid();

        task.AssignTo(userId);
        Assert.Equal(userId, task.AssignedToId);

        task.Unassign();
        Assert.Null(task.AssignedToId);
    }

    [Fact]
    public void Nao_deve_atribuir_usuario_vazio()
    {
        Assert.Throws<DomainException>(() => NewTask().AssignTo(Guid.Empty));
    }
}
