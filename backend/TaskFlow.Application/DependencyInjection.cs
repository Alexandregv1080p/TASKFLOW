using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.ForYou;
using TaskFlow.Application.Goals;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Search;
using TaskFlow.Application.Spaces;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<ProjectMemberService>();
        services.AddScoped<ProjectExportService>();
        services.AddScoped<SpaceService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TaskTypeService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ForYouService>();
        services.AddScoped<GoalService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<SearchService>();

        return services;
    }
}
