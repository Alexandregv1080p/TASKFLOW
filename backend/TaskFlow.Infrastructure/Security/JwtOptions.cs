namespace TaskFlow.Infrastructure.Security;

// Seção "Jwt" da configuração. O Secret nunca fica no appsettings.json: em produção vem de Jwt__Secret.
public class JwtOptions
{
    public const int MinSecretLength = 32;

    public string Issuer { get; set; } = "taskflow";

    public string Audience { get; set; } = "taskflow-web";

    public string Secret { get; set; } = "";

    public int ExpirationMinutes { get; set; } = 60;
}
