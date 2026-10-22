using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common;
using TaskFlow.Application.Webhooks;
using TaskFlow.Infrastructure.Antivirus;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;
using TaskFlow.Infrastructure.Storage;
using TaskFlow.Infrastructure.Webhooks;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwt,
        StorageOptions storage,
        string defaultStoragePath,
        AntivirusOptions antivirus,
        WebhookOptions webhooks)
    {
        if (jwt.Secret.Length < JwtOptions.MinSecretLength)
            throw new InvalidOperationException($"Jwt:Secret must have at least {JwtOptions.MinSecretLength} characters.");

        // O outbox dos webhooks entra em todo SaveChanges (ver WebhookOutbox).
        services.AddDbContext<TaskFlowDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .AddInterceptors(sp.GetRequiredService<WebhookOutbox>()));
        services.AddScoped<ITaskFlowDbContext>(sp => sp.GetRequiredService<TaskFlowDbContext>());

        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        // Anexos: pasta local (uma instância) ou S3 (várias réplicas, ou para não depender do disco do servidor).
        services.AddSingleton<IFileStorage>(storage.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase)
            ? new S3FileStorage(storage.S3)
            : new LocalFileStorage(storage.Path ?? defaultStoragePath));

        // Antivírus nos anexos: ClamAV quando há host configurado; senão, nada é verificado (a API avisa no log).
        services.AddSingleton<IVirusScanner>(string.IsNullOrWhiteSpace(antivirus.Host) ? new NoVirusScanner() : new ClamAvScanner(antivirus));

        services.AddSingleton(webhooks);
        services.AddSingleton<WebhookDispatcher>();

        return services;
    }
}
