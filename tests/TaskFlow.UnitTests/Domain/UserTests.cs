using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Deve_normalizar_email()
    {
        var user = new User("Ana", "  Ana@Example.COM ", "hash");

        Assert.Equal("ana@example.com", user.Email);
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("Ana <ana@example.com>")]
    [InlineData("")]
    public void Deve_rejeitar_email_invalido(string email)
    {
        Assert.Throws<DomainException>(() => new User("Ana", email, "hash"));
    }

    [Fact]
    public void Deve_exigir_hash_de_senha()
    {
        Assert.Throws<DomainException>(() => new User("Ana", "ana@example.com", ""));
    }
}
