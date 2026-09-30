namespace TaskFlow.Application.Common;

public class NotFoundException(string entity, Guid id) : Exception($"{entity} '{id}' was not found.");
