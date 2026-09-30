using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProjectService>();
        services.AddScoped<TaskService>();
        services.AddScoped<DashboardService>();

        return services;
    }
}
