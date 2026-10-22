using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Webhooks;

// Outbox dos webhooks: logo antes de cada SaveChanges, olha o que mudou (eventos do histórico, comentários novos,
// tarefas excluídas) e acrescenta as entregas para os webhooks ativos do projeto. Tudo vai na mesma transação:
// se a alteração foi gravada, o evento também foi; se a API cair depois, o despachante entrega quando voltar.
// Um lugar só para todos os fluxos: qualquer alteração que registra histórico já gera o evento.
// ponytail: só o SaveChangesAsync passa por aqui (todo o código usa a versão assíncrona); e uma consulta a mais
// por gravação que envolve tarefas, para achar os webhooks. Cachear se isso pesar.
public class WebhookOutbox(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private record Change(Guid TaskId, Guid? ActorId, string Event, object? Detail, DateTime OccurredAt);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ITaskFlowDbContext db)
            await EnqueueAsync(eventData.Context, db, cancellationToken);

        return result;
    }

    private async Task EnqueueAsync(DbContext context, ITaskFlowDbContext db, CancellationToken ct)
    {
        var changes = Collect(context);
        if (changes.Count == 0)
            return;

        // A tarefa pode estar no contexto (criada, alterada, excluída agora) ou só no banco (ex.: comentário novo).
        var tracked = context.ChangeTracker.Entries<TaskItem>().ToDictionary(e => e.Entity.Id, e => e.Entity);
        var missing = changes.Select(c => c.TaskId).Distinct().Where(id => !tracked.ContainsKey(id)).ToList();
        var tasks = missing.Count == 0
            ? tracked
            : tracked.Values.Concat(await db.Tasks.AsNoTracking().Where(t => missing.Contains(t.Id)).ToListAsync(ct))
                .ToDictionary(t => t.Id);

        var projectIds = tasks.Values.Select(t => t.ProjectId).Distinct().ToList();
        var webhooks = await db.Webhooks.AsNoTracking()
            .Where(w => w.Active && projectIds.Contains(w.ProjectId))
            .Select(w => new { w.Id, w.ProjectId })
            .ToListAsync(ct);
        if (webhooks.Count == 0)
            return;

        var projects = await db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var actorIds = changes.Where(c => c.ActorId is not null).Select(c => c.ActorId!.Value).Distinct().ToList();
        var actors = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        foreach (var change in changes)
        {
            if (!tasks.TryGetValue(change.TaskId, out var task))
                continue;

            var projectName = projects.GetValueOrDefault(task.ProjectId, "");
            var payload = Serialize(new
            {
                @event = change.Event,
                occurredAt = change.OccurredAt,
                project = new { id = task.ProjectId, name = projectName, key = ProjectKey.For(projectName) },
                task = new
                {
                    id = task.Id,
                    key = ProjectKey.TaskKey(projectName, task.Number),
                    task.Title,
                    task.Status,
                    task.Priority,
                    task.DueDate,
                    assigneeId = task.AssignedToId,
                    task.TypeId,
                    task.ParentId,
                },
                actor = change.ActorId is { } actorId ? new { id = actorId, name = actors.GetValueOrDefault(actorId) } : null,
                change = change.Detail,
            });

            foreach (var webhook in webhooks.Where(w => w.ProjectId == task.ProjectId))
                db.WebhookDeliveries.Add(new WebhookDelivery(webhook.Id, change.Event, payload));
        }
    }

    private List<Change> Collect(DbContext context)
    {
        var changes = new List<Change>();

        foreach (var entry in context.ChangeTracker.Entries<TaskEvent>().Where(e => e.State == EntityState.Added))
        {
            var e = entry.Entity;
            (string Name, object? Detail) mapped = e.Type switch
            {
                TaskEventType.Created => ("task.created", null),
                TaskEventType.StatusChanged => ("task.status_changed", Detail("status", e)),
                TaskEventType.AssigneeChanged => ("task.assigned", Detail("assignee", e)),
                TaskEventType.AttachmentAdded => ("attachment.added", new { fileName = e.NewValue }),
                TaskEventType.AttachmentRemoved => ("attachment.removed", new { fileName = e.OldValue }),
                _ => ("task.updated", Detail(FieldName(e.Type), e)),
            };
            changes.Add(new Change(e.TaskId, e.UserId, mapped.Name, mapped.Detail, e.CreatedAt));
        }

        foreach (var entry in context.ChangeTracker.Entries<TaskComment>().Where(e => e.State == EntityState.Added))
        {
            var c = entry.Entity;
            changes.Add(new Change(c.TaskId, c.AuthorId, "comment.created", new { commentId = c.Id, body = c.Body }, c.CreatedAt));
        }

        var deleted = context.ChangeTracker.Entries<TaskItem>().Where(e => e.State == EntityState.Deleted).ToList();
        if (deleted.Count > 0)
        {
            // Excluir não registra histórico (o histórico some junto): quem excluiu é o usuário da requisição.
            Guid? actor = null;
            try
            {
                actor = currentUser.UserId;
            }
            catch (InvalidOperationException)
            {
            }

            changes.AddRange(deleted.Select(e => new Change(e.Entity.Id, actor, "task.deleted", null, DateTime.UtcNow)));
        }

        return changes;
    }

    private static object Detail(string field, TaskEvent e) => new { field, from = e.OldValue, to = e.NewValue };

    private static string FieldName(TaskEventType type) => type switch
    {
        TaskEventType.TitleChanged => "title",
        TaskEventType.DescriptionChanged => "description",
        TaskEventType.PriorityChanged => "priority",
        TaskEventType.DueDateChanged => "dueDate",
        TaskEventType.TypeChanged => "type",
        TaskEventType.ParentChanged => "parent",
        _ => type.ToString(),
    };

    public static string Serialize(object payload) => JsonSerializer.Serialize(payload, Json);
}
