namespace TaskFlow.Application.Common;

// Usuário autenticado da requisição atual (vem do JWT).
public interface ICurrentUser
{
    Guid UserId { get; }
}
