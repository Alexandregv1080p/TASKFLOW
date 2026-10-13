using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common;

namespace TaskFlow.Api.Hubs;

// Tempo real: cada projeto é um grupo, e quem abre o quadro entra no grupo dele; cada pessoa tem o seu grupo,
// por onde chega o aviso de notificação nova. O nome vem do primeiro uso (o quadro); é o único hub da aplicação.
// O servidor só avisa "mudou" (ids, sem dados); o cliente recarrega pela API REST, que aplica as regras de acesso.
public class BoardHub(ITaskFlowDbContext db) : Hub
{
    public const string Changed = "BoardChanged";

    public const string Notified = "Notified";

    public static string Group(Guid projectId) => $"project:{projectId}";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    private Guid UserId => Guid.Parse(Context.User!.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(UserId));
        await base.OnConnectedAsync();
    }

    // Mesma regra do REST: só dono ou membro, e sem acesso o projeto "não existe".
    // ponytail: quem é removido do projeto continua no grupo até fechar a página; recebe só ids e o
    // recarregamento dele responde 404. Para cortar na hora, guardar as conexões por usuário e removê-las do grupo.
    public async Task JoinProject(Guid projectId)
    {
        if (!await db.IsProjectPersonAsync(projectId, UserId, Context.ConnectionAborted))
            throw new HubException("Project not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(projectId));
    }

    // A conexão é uma por aba e continua aberta depois de sair do quadro.
    public Task LeaveProject(Guid projectId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(projectId));
}

// ponytail: grupos em memória servem para uma instância da API; com várias réplicas, ligar um backplane
// (AddStackExchangeRedis) para o aviso chegar a conexões abertas em outra instância.
public class SignalRLiveNotifier(IHubContext<BoardHub> hub, ILogger<SignalRLiveNotifier> logger) : ILiveNotifier
{
    public Task ProjectChangedAsync(Guid projectId, Guid? taskId = null) =>
        SendAsync(hub.Clients.Group(BoardHub.Group(projectId)), BoardHub.Changed, new { projectId, taskId });

    public Task UsersNotifiedAsync(IReadOnlyCollection<Guid> userIds) =>
        SendAsync(hub.Clients.Groups(userIds.Select(BoardHub.UserGroup)), BoardHub.Notified, new { });

    // A alteração já foi gravada: falhar ao avisar não pode virar erro para quem a fez.
    private async Task SendAsync(IClientProxy clients, string method, object payload)
    {
        try
        {
            await clients.SendAsync(method, payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LiveNotifyFailed {Method}", method);
        }
    }
}
