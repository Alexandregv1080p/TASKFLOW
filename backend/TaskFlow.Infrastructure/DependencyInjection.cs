using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;
using TaskFlow.Infrastructure.Storage;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, JwtOptions jwt, string storagePath)
    {
        if (jwt.Secret.Length < JwtOptions.MinSecretLength)
            throw new InvalidOperationException($"Jwt:Secret must have at least {JwtOptions.MinSecretLength} characters.");

        services.AddDbContext<TaskFlowDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITaskFlowDbContext>(sp => sp.GetRequiredService<TaskFlowDbContext>());

        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storagePath));

        return services;
    }
}
