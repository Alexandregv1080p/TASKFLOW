using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Auth;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Name, string Email)
{
    public static UserResponse From(User u) => new(u.Id, u.Name, u.Email);
}

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);

public class AuthService(
    ITaskFlowDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    ICurrentUser currentUser,
    ILogger<AuthService> logger)
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.Password is not { Length: >= PasswordMinLength and <= PasswordMaxLength })
            throw new DomainException($"Password must have between {PasswordMinLength} and {PasswordMaxLength} characters.");

        var email = Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email is already registered.");

        var user = new User(request.Name, email, hasher.Hash(request.Password));

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("UserRegistered {UserId}", user.Id);

        return Issue(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

        // Mesma resposta para e-mail inexistente e senha errada: não revela quais e-mails existem.
        if (user is null || !hasher.Verify(user.PasswordHash, request.Password))
        {
            // Sem o e-mail tentado: não coloca dado pessoal nem pista de enumeração nos logs.
            logger.LogWarning("LoginFailed");
            TaskFlowMetrics.Logins.Add(1, new KeyValuePair<string, object?>("result", "failure"));
            throw new InvalidCredentialsException();
        }

        logger.LogInformation("LoginSucceeded {UserId}", user.Id);
        TaskFlowMetrics.Logins.Add(1, new KeyValuePair<string, object?>("result", "success"));

        return Issue(user);
    }

    public async Task<UserResponse> MeAsync(CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([currentUser.UserId], ct)
            ?? throw new NotFoundException(nameof(User), currentUser.UserId);

        return UserResponse.From(user);
    }

    private AuthResponse Issue(User user)
    {
        var (token, expiresAt) = tokens.Create(user);

        return new AuthResponse(token, expiresAt, UserResponse.From(user));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
