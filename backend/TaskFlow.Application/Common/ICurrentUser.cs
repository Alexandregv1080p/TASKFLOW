namespace TaskFlow.Application.Common;

public interface ICurrentUser
{
    Guid UserId { get; }
}
