using System.Diagnostics.Metrics;

namespace TaskFlow.Application.Common;

// Métricas de negócio com a API padrão do .NET (System.Diagnostics.Metrics).
// A Application não conhece o Prometheus: quem exporta é a camada de API.
public static class TaskFlowMetrics
{
    public const string MeterName = "TaskFlow";

    private static readonly Meter Meter = new(MeterName);

    // O exportador prefixa o nome do meter: no Prometheus ficam taskflow_projects_created etc.
    public static readonly Counter<long> ProjectsCreated = Meter.CreateCounter<long>("projects_created");

    public static readonly Counter<long> TasksCreated = Meter.CreateCounter<long>("tasks_created");

    public static readonly Counter<long> TasksCompleted = Meter.CreateCounter<long>("tasks_completed");

    public static readonly Counter<long> Logins = Meter.CreateCounter<long>("logins");
}
