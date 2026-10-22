using System.Security.Cryptography;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Endereço que recebe os eventos de um projeto (integração de saída). O dono do projeto cadastra; cada envio é
// assinado com o segredo, para quem recebe conferir que veio do TaskFlow e não foi alterado no caminho.
public class Webhook : Entity
{
    public const int UrlMaxLength = 500;
    public const int MaxPerProject = 5;

    public Guid ProjectId { get; private set; }
    public string Url { get; private set; }

    // Chave do HMAC. Fica em texto no banco porque é preciso tê-la para assinar (não dá para guardar só o hash).
    public string Secret { get; private set; }

    // Desativado não recebe eventos novos; o histórico de entregas continua.
    public bool Active { get; private set; }

    public Webhook(Guid projectId, string url)
    {
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        Url = ValidUrl(url);
        Secret = NewSecret();
        Active = true;
    }

    // Usado pelo EF Core ao materializar do banco.
    private Webhook()
    {
        Url = null!;
        Secret = null!;
    }

    public void Update(string url, bool active)
    {
        Url = ValidUrl(url);
        Active = active;
    }

    // Segredo vazou ou a pessoa quer trocar: o antigo deixa de valer na hora.
    public void RotateSecret() => Secret = NewSecret();

    private static string NewSecret() => "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    // Só http(s) absoluto e sem usuário/senha embutidos. Endereços internos são barrados na hora do envio
    // (o nome pode apontar para outro IP depois do cadastro), não aqui.
    private static string ValidUrl(string? url)
    {
        var value = Guard.Required(url, UrlMaxLength, nameof(Url));

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.UserInfo))
            throw new DomainException("Url must be an absolute http or https address, like https://example.com/hooks/taskflow.");

        return value;
    }
}
