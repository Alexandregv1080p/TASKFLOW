using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TaskFlowDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITaskFlowDbContext>(sp => sp.GetRequiredService<TaskFlowDbContext>());

        return services;
    }
}
