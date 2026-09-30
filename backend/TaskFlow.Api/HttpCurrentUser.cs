using System.IdentityModel.Tokens.Jwt;
using TaskFlow.Application.Common;

namespace TaskFlow.Api;

// Lê o usuário do claim "sub" do JWT validado pelo middleware de autenticação.
public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new InvalidOperationException("No authenticated user in the current request.");
}
