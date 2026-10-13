namespace TaskFlow.Application.Common;

// Avisos em tempo real. A API implementa com SignalR; a camada de aplicação só conhece esta interface.
// Os avisos não levam dados: quem recebe recarrega pela API REST.
public interface ILiveNotifier
{
    // Algo mudou no projeto: avisa quem está com o quadro dele aberto.
    Task ProjectChangedAsync(Guid projectId, Guid? taskId = null);

    // Estas pessoas têm notificação nova: avisa as sessões abertas delas.
    Task UsersNotifiedAsync(IReadOnlyCollection<Guid> userIds);
}

public static class LiveNotifierExtensions
{
    // Sem notificador registrado (services criados direto nos testes), não há quem avisar.
    public static Task ChangedAsync(this ILiveNotifier? live, Guid projectId, Guid? taskId = null) =>
        live?.ProjectChangedAsync(projectId, taskId) ?? Task.CompletedTask;

    public static Task NotifiedAsync(this ILiveNotifier? live, IReadOnlyCollection<Guid> userIds) =>
        live is null || userIds.Count == 0 ? Task.CompletedTask : live.UsersNotifiedAsync(userIds);
}
