using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(User user);
}

public class ConflictException(string message) : Exception(message);

public class InvalidCredentialsException() : Exception("Invalid email or password.");
