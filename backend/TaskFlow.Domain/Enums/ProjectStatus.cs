namespace TaskFlow.Domain.Enums;

// Situação geral do projeto, informada pelo dono (como as atualizações de status do Jira).
public enum ProjectStatus
{
    OnTrack,
    AtRisk,
    OffTrack,
    Paused,
    Completed
}
