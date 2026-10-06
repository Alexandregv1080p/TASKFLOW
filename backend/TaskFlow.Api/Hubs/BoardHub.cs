using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common;

namespace TaskFlow.Api.Hubs;

// Tempo real do quadro: cada projeto é um grupo, e quem abre o quadro entra no grupo dele.
// O servidor só avisa "mudou" (ids, sem dados); o cliente recarrega pela API REST, que aplica as regras de acesso.
public class BoardHub(ITaskFlowDbContext db) : Hub
{
    public const string Changed = "BoardChanged";

    public static string Group(Guid projectId) => $"project:{projectId}";

    // Mesma regra do REST: só dono ou membro, e sem acesso o projeto "não existe".
    // ponytail: quem é removido do projeto continua no grupo até fechar a página; recebe só ids e o
    // recarregamento dele responde 404. Para cortar na hora, guardar as conexões por usuário e removê-las do grupo.
    public async Task JoinProject(Guid projectId)
    {
        var userId = Guid.Parse(Context.User!.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

        if (!await db.IsProjectPersonAsync(projectId, userId, Context.ConnectionAborted))
            throw new HubException("Project not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(projectId));
    }
}

// ponytail: grupos em memória servem para uma instância da API; com várias réplicas, ligar um backplane
// (AddStackExchangeRedis) para o aviso chegar a conexões abertas em outra instância.
public class SignalRBoardNotifier(IHubContext<BoardHub> hub, ILogger<SignalRBoardNotifier> logger) : IBoardNotifier
{
    public async Task ProjectChangedAsync(Guid projectId, Guid? taskId = null)
    {
        // A alteração já foi gravada: falhar ao avisar não pode virar erro para quem a fez.
        try
        {
            await hub.Clients.Group(BoardHub.Group(projectId)).SendAsync(BoardHub.Changed, new { projectId, taskId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BoardNotifyFailed {ProjectId}", projectId);
        }
    }
}
