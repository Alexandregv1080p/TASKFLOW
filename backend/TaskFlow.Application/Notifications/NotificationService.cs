using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Notifications;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string ActorName,
    Guid TaskId,
    Guid ProjectId,
    string ProjectName,
    int TaskNumber,
    string TaskTitle,
    DateTime CreatedAt,
    bool Read);

public record NotificationListResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);

// Notificações do usuário atual. Elas são criadas pelo TaskService, na mesma transação da ação que as gera.
public class NotificationService(ITaskFlowDbContext db, ICurrentUser currentUser)
{
    private const int PageSize = 30;

    // ponytail: só as 30 mais recentes, sem paginação nem limpeza das antigas; paginar e expirar se o volume pedir.
    public async Task<NotificationListResponse> ListAsync(CancellationToken ct = default)
    {
        var mine = Mine();

        var items = await mine.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(PageSize)
            .Join(db.Tasks, n => n.TaskId, t => t.Id, (n, t) => new NotificationResponse(
                n.Id,
                n.Type,
                db.Users.Where(u => u.Id == n.ActorId).Select(u => u.Name).First(),
                t.Id,
                t.ProjectId,
                db.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).First(),
                t.Number,
                t.Title,
                n.CreatedAt,
                n.ReadAt != null))
            .ToListAsync(ct);

        return new NotificationListResponse(await mine.CountAsync(n => n.ReadAt == null, ct), items);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await Mine().SingleOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new NotFoundException(nameof(Notification), id);

        notification.MarkRead();
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        foreach (var notification in await Mine().Where(n => n.ReadAt == null).ToListAsync(ct))
            notification.MarkRead();

        await db.SaveChangesAsync(ct);
    }

    // Só de tarefas em projetos a que a pessoa ainda tem acesso: quem saiu do projeto deixa de ver os avisos dele.
    private IQueryable<Notification> Mine()
    {
        var userId = currentUser.UserId;
        var projects = db.AccessibleProjects(userId);

        return db.Notifications.Where(n =>
            n.UserId == userId && db.Tasks.Any(t => t.Id == n.TaskId && projects.Any(p => p.Id == t.ProjectId)));
    }
}
