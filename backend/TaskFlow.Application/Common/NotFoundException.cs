namespace TaskFlow.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, Guid id) : base($"{entity} '{id}' was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
