using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Common;

namespace TaskFlow.Infrastructure.Security;

// Adapta o PasswordHasher do ASP.NET Core Identity (PBKDF2 com salt, versionado) sem trazer o Identity inteiro.
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
