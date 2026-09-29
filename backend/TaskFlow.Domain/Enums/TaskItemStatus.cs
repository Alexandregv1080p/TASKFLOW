namespace TaskFlow.Domain.Enums;

// Não se chama TaskStatus para não colidir com System.Threading.Tasks.TaskStatus.
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done
}
