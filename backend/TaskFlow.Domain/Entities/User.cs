using System.Net.Mail;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public class User : Entity
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public string Name { get; private set; }
    public string Email { get; private set; }

    // O hash é gerado fora do domínio; a senha em texto puro nunca chega aqui.
    public string PasswordHash { get; private set; }

    public User(string name, string email, string passwordHash)
    {
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Email = NormalizeEmail(email);
        PasswordHash = Guard.Required(passwordHash, int.MaxValue, nameof(PasswordHash));
    }

    private static string NormalizeEmail(string email)
    {
        var value = Guard.Required(email, EmailMaxLength, nameof(Email)).ToLowerInvariant();

        if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
            throw new DomainException("Email is invalid.");

        return value;
    }
}
