using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Api;

// ponytail: usuário fixo, criado só em Development, até a autenticação JWT (Fase 7) substituir esta classe.
public class DemoCurrentUser : ICurrentUser
{
    private const string Email = "demo@taskflow.local";

    public Guid UserId { get; private set; }

    public async Task EnsureSeededAsync(TaskFlowDbContext db)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == Email);

        if (user is null)
        {
            // Hash inválido de propósito: o usuário demo não consegue fazer login.
            user = new User("Usuário Demo", Email, "!");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        UserId = user.Id;
    }
}
