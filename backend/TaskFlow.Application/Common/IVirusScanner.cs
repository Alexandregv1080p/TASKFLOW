namespace TaskFlow.Application.Common;

public record ScanResult(bool Clean, string? Threat = null)
{
    public static readonly ScanResult Ok = new(true);
}

// Verifica arquivos enviados antes de gravar. A implementação real é o ClamAV (ClamAvScanner); sem antivírus
// configurado, NoVirusScanner deixa passar (ambiente de desenvolvimento) e a API avisa no log ao subir.
public interface IVirusScanner
{
    // Lança ServiceUnavailableException se o antivírus estiver configurado mas não responder.
    Task<ScanResult> ScanAsync(Stream content, CancellationToken ct = default);
}

public sealed class NoVirusScanner : IVirusScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken ct = default) => Task.FromResult(ScanResult.Ok);
}

// Uma dependência externa necessária não respondeu (ex.: antivírus fora do ar). A API responde 503.
public class ServiceUnavailableException(string message) : Exception(message);
