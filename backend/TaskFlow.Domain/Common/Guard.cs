using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        return MaxLength(value, maxLength, field)!;
    }

    public static string? Optional(string? value, int maxLength, string field) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value, maxLength, field);

    public static Guid NotEmpty(Guid value, string field) =>
        value == Guid.Empty ? throw new DomainException($"{field} is required.") : value;

    private static string? MaxLength(string value, int maxLength, string field)
    {
        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must have at most {maxLength} characters.");

        return trimmed;
    }
}
