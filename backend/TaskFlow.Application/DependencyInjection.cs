using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.ForYou;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<ProjectMemberService>();
        services.AddScoped<TaskService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ForYouService>();

        return services;
    }
}
