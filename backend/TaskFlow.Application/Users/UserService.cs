using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Users;

// Só id e nome: suficiente para escolher responsável, sem expor e-mails.
public record UserSummaryResponse(Guid Id, string Name);

public class UserService(ITaskFlowDbContext db)
{
    public async Task<IReadOnlyList<UserSummaryResponse>> ListAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UserSummaryResponse(u.Id, u.Name))
            .ToListAsync(ct);
}
