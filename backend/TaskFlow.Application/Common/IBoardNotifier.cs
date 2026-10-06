namespace TaskFlow.Application.Common;

// Avisa quem está com o quadro de um projeto aberto de que algo mudou. A API implementa com SignalR;
// a camada de aplicação só conhece esta interface.
public interface IBoardNotifier
{
    Task ProjectChangedAsync(Guid projectId, Guid? taskId = null);
}

public static class BoardNotifierExtensions
{
    // Sem notificador registrado (services criados direto nos testes), não há quem avisar.
    public static Task ChangedAsync(this IBoardNotifier? board, Guid projectId, Guid? taskId = null) =>
        board?.ProjectChangedAsync(projectId, taskId) ?? Task.CompletedTask;
}
