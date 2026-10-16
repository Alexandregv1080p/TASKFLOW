using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Tasks;

// Tarefas só são acessíveis através de projetos em que o usuário atual é dono ou membro.
// Em projeto arquivado, as tarefas ficam somente leitura (estrela e "visto" são pessoais e continuam valendo).
public class TaskService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<TaskService> logger, ILiveNotifier? live = null)
{
    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var project = await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);
        project.EnsureActive();
        await EnsureCanEditAsync(projectId, ct);

        // ponytail: próximo número via MAX+1; duas criações simultâneas no mesmo projeto colidem no índice único
        // (ProjectId, Number) e uma delas falha. Trocar por sequência por projeto se houver concorrência real.
        var number = (await db.Tasks.Where(t => t.ProjectId == projectId).MaxAsync(t => (int?)t.Number, ct) ?? 0) + 1;

        var task = new TaskItem(
            projectId, number, currentUser.UserId, request.Title, request.Description, request.Priority, request.DueDate);

        if (request.ParentId is { } parentId)
            task.MakeSubtaskOf(await FindAsync(parentId, ct));

        if (request.TypeId is { } typeId)
            task.ChangeType(await EnsureTypeAsync(projectId, typeId, ct));

        db.Tasks.Add(task);
        Record(task, TaskEventType.Created);

        // Já nasce com responsável: mesmo histórico e mesmo aviso de uma atribuição feita depois.
        Guid[] notified = [];
        if (request.AssignedToId is { } assigneeId)
        {
            if (!await db.CanEditProjectAsync(projectId, assigneeId, ct))
                throw new NotFoundException(nameof(User), assigneeId);

            task.AssignTo(assigneeId);
            Record(task, TaskEventType.AssigneeChanged, null, assigneeId.ToString());
            notified = Notify(task, NotificationType.Assigned, [assigneeId]);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("TaskCreated {TaskId} {ProjectId} {UserId}", task.Id, projectId, currentUser.UserId);
        TaskFlowMetrics.TasksCreated.Add(1);
        await live.ChangedAsync(projectId, task.Id);
        await live.NotifiedAsync(notified);

        return TaskResponse.From(task, starred: false);
    }

    public async Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Number)
            .ToListAsync(ct);

        var userId = currentUser.UserId;
        var starred = (await db.TaskStars
                .Where(s => s.UserId == userId && db.Tasks.Any(t => t.Id == s.TaskId && t.ProjectId == projectId))
                .Select(s => s.TaskId)
                .ToListAsync(ct))
            .ToHashSet();

        return tasks.Select(t => TaskResponse.From(t, starred.Contains(t.Id))).ToList();
    }

    public async Task<TaskResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        await ResponseAsync(await FindAsync(id, ct), ct);

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);
        var (title, description, priority, dueDate) = (task.Title, task.Description, task.Priority, task.DueDate);

        task.Update(request.Title, request.Description, request.DueDate);
        task.ChangePriority(request.Priority);

        // Um evento por campo que realmente mudou (comparando o valor já normalizado pelo domínio).
        if (task.Title != title)
            Record(task, TaskEventType.TitleChanged, title, task.Title);
        if (task.Description != description)
            Record(task, TaskEventType.DescriptionChanged); // sem o texto: pode ser longo e o atual já está na tarefa
        if (task.Priority != priority)
            Record(task, TaskEventType.PriorityChanged, priority.ToString(), task.Priority.ToString());
        if (task.DueDate?.Date != dueDate?.Date)
            Record(task, TaskEventType.DueDateChanged, FormatDate(dueDate), FormatDate(task.DueDate));

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);

        return await ResponseAsync(task, ct);
    }

    public async Task<IReadOnlyList<TaskEventResponse>> HistoryAsync(Guid id, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);

        return await db.TaskEvents.AsNoTracking()
            .Where(e => e.TaskId == task.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new TaskEventResponse(e.Id, e.Type, e.OldValue, e.NewValue, e.UserId, e.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);
        var oldStatus = task.Status;

        task.ChangeStatus(request.Status);

        if (oldStatus != task.Status)
            Record(task, TaskEventType.StatusChanged, oldStatus.ToString(), task.Status.ToString());

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);

        if (oldStatus != task.Status)
        {
            logger.LogInformation(
                "TaskStatusChanged {TaskId} {OldStatus} {NewStatus} {UserId}", task.Id, oldStatus, task.Status, currentUser.UserId);

            if (task.Status == TaskItemStatus.Done)
                TaskFlowMetrics.TasksCompleted.Add(1);
        }

        return await ResponseAsync(task, ct);
    }

    public async Task<TaskResponse> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);
        var oldAssignee = task.AssignedToId;

        if (request.UserId is not { } userId)
        {
            task.Unassign();
        }
        else
        {
            // Só quem está no projeto (dono ou membro) pode ser responsável.
            if (!await db.CanEditProjectAsync(task.ProjectId, userId, ct))
                throw new NotFoundException(nameof(User), userId);

            task.AssignTo(userId);
        }

        Guid[] notified = [];

        if (task.AssignedToId != oldAssignee)
        {
            Record(task, TaskEventType.AssigneeChanged, oldAssignee?.ToString(), task.AssignedToId?.ToString());

            if (task.AssignedToId is { } assignee)
                notified = Notify(task, NotificationType.Assigned, [assignee]);
        }

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);
        await live.NotifiedAsync(notified);

        return await ResponseAsync(task, ct);
    }

    public async Task<TaskResponse> ChangeTypeAsync(Guid id, ChangeTaskTypeRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);
        var oldType = task.TypeId;

        task.ChangeType(request.TypeId is { } typeId ? await EnsureTypeAsync(task.ProjectId, typeId, ct) : null);

        if (task.TypeId != oldType)
            Record(task, TaskEventType.TypeChanged, oldType?.ToString(), task.TypeId?.ToString());

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);

        return await ResponseAsync(task, ct);
    }

    // Transforma uma tarefa existente em subtarefa (ou o contrário). Um nível só: quem já tem subtarefas
    // não vira subtarefa, e o pai não pode ser uma subtarefa (regra no domínio).
    public async Task<TaskResponse> ChangeParentAsync(Guid id, ChangeTaskParentRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(id, ct);
        var oldParent = task.ParentId;

        if (request.ParentId is { } parentId)
        {
            if (await db.Tasks.AnyAsync(t => t.ParentId == task.Id, ct))
                throw new DomainException("A task that has subtasks cannot become a subtask.");

            task.MakeSubtaskOf(await FindAsync(parentId, ct));
        }
        else
        {
            task.DetachFromParent();
        }

        if (task.ParentId != oldParent)
            Record(task, TaskEventType.ParentChanged, oldParent?.ToString(), task.ParentId?.ToString());

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);

        return await ResponseAsync(task, ct);
    }

    // Estrela: marcação pessoal. Marcar duas vezes ou desmarcar o que não está marcado não é erro.
    public async Task<TaskResponse> SetStarAsync(Guid id, bool starred, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var userId = currentUser.UserId;
        var star = await db.TaskStars.SingleOrDefaultAsync(s => s.UserId == userId && s.TaskId == task.Id, ct);

        if (starred && star is null)
            db.TaskStars.Add(new TaskStar(userId, task.Id));
        else if (!starred && star is not null)
            db.TaskStars.Remove(star);

        await db.SaveChangesAsync(ct);

        return TaskResponse.From(task, starred);
    }

    // Registra que o usuário abriu a tarefa (alimenta a aba "Visto" da página Para você).
    public async Task MarkViewedAsync(Guid id, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var userId = currentUser.UserId;
        var view = await db.TaskViews.SingleOrDefaultAsync(v => v.UserId == userId && v.TaskId == task.Id, ct);

        if (view is null)
            db.TaskViews.Add(new TaskView(userId, task.Id));
        else
            view.Touch();

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TaskCommentResponse>> ListCommentsAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await FindAsync(taskId, ct);

        var comments = await db.TaskComments.AsNoTracking()
            .Where(c => c.TaskId == task.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return comments.Select(TaskCommentResponse.From).ToList();
    }

    public async Task<TaskCommentResponse> AddCommentAsync(Guid taskId, AddCommentRequest request, CancellationToken ct = default)
    {
        var task = await FindForWriteAsync(taskId, ct);
        var comment = new TaskComment(task.Id, currentUser.UserId, request.Body);

        db.TaskComments.Add(comment);

        // Menção: "@Nome" de alguém do projeto no texto. Responsável e criador são avisados de qualquer
        // comentário; quem foi mencionado recebe só o aviso de menção.
        // ponytail: casa pelo nome completo dentro do texto, então "@Ana Paula" também avisa uma "Ana" do mesmo
        // projeto. Para precisão, o cliente enviaria os ids mencionados junto com o comentário.
        var people = await db.Users
            .Where(u => db.Projects.Any(p => p.Id == task.ProjectId && p.OwnerId == u.Id)
                || db.ProjectMembers.Any(m => m.ProjectId == task.ProjectId && m.UserId == u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToListAsync(ct);
        var mentioned = people
            .Where(p => comment.Body.Contains('@' + p.Name, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Id)
            .ToList();
        var watchers = new[] { task.AssignedToId, task.CreatedById }
            .OfType<Guid>()
            .Where(id => !mentioned.Contains(id) && people.Any(p => p.Id == id));
        Guid[] notified =
            [.. Notify(task, NotificationType.Mentioned, mentioned), .. Notify(task, NotificationType.Commented, watchers)];

        await db.SaveChangesAsync(ct);

        logger.LogInformation("CommentAdded {CommentId} {TaskId} {UserId}", comment.Id, task.Id, currentUser.UserId);
        await live.ChangedAsync(task.ProjectId, task.Id);
        await live.NotifiedAsync(notified);

        return TaskCommentResponse.From(comment);
    }

    // Só o autor exclui o próprio comentário. Comentário de outra pessoa, ou em tarefa sem acesso,
    // responde 404: não revela que ele existe.
    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await db.TaskComments.SingleOrDefaultAsync(
                c => c.Id == commentId
                    && c.AuthorId == currentUser.UserId
                    && Accessible().Any(t => t.Id == c.TaskId),
                ct)
            ?? throw new NotFoundException(nameof(TaskComment), commentId);

        var task = await FindForWriteAsync(comment.TaskId, ct);

        db.TaskComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(task.ProjectId, task.Id);
    }

    // O evento entra no mesmo SaveChanges da alteração: ou os dois são gravados, ou nenhum.
    private void Record(TaskItem task, TaskEventType type, string? oldValue = null, string? newValue = null) =>
        db.TaskEvents.Add(new TaskEvent(task.Id, currentUser.UserId, type, oldValue, newValue));

    // Cria as notificações no mesmo SaveChanges da ação e devolve quem deve ser avisado em tempo real.
    // Ninguém é notificado do que ele mesmo fez.
    private Guid[] Notify(TaskItem task, NotificationType type, IEnumerable<Guid> userIds)
    {
        var recipients = userIds.Where(id => id != currentUser.UserId).Distinct().ToArray();

        foreach (var userId in recipients)
            db.Notifications.Add(new Notification(userId, currentUser.UserId, task.Id, type));

        return recipients;
    }

    // Tipo de outro projeto (ou inexistente) responde 404: não revela tipos de projetos alheios.
    private async Task<Guid> EnsureTypeAsync(Guid projectId, Guid typeId, CancellationToken ct) =>
        await db.TaskTypes.AnyAsync(t => t.Id == typeId && t.ProjectId == projectId, ct)
            ? typeId
            : throw new NotFoundException(nameof(TaskType), typeId);

    private static string? FormatDate(DateTime? date) => date?.ToString("yyyy-MM-dd");

    private async Task<TaskResponse> ResponseAsync(TaskItem task, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        return TaskResponse.From(task, await db.TaskStars.AnyAsync(s => s.UserId == userId && s.TaskId == task.Id, ct));
    }

    // Tarefas dos projetos em que o usuário é dono ou membro.
    private IQueryable<TaskItem> Accessible()
    {
        var projects = db.AccessibleProjects(currentUser.UserId);

        return db.Tasks.Where(t => projects.Any(p => p.Id == t.ProjectId));
    }

    private async Task<TaskItem> FindAsync(Guid id, CancellationToken ct) =>
        await Accessible().SingleOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException(nameof(TaskItem), id);

    // Para alterações: além do acesso, o projeto não pode estar arquivado.
    private async Task<TaskItem> FindForWriteAsync(Guid id, CancellationToken ct)
    {
        var task = await FindAsync(id, ct);

        if (await db.Projects.AnyAsync(p => p.Id == task.ProjectId && p.ArchivedAt != null, ct))
            throw new DomainException("This project is archived. Restore it to make changes.");

        await EnsureCanEditAsync(task.ProjectId, ct);

        return task;
    }

    // Visualizador vê tudo, mas não altera: 403 (ele já sabe que o projeto existe).
    private async Task EnsureCanEditAsync(Guid projectId, CancellationToken ct)
    {
        if (!await db.CanEditProjectAsync(projectId, currentUser.UserId, ct))
            throw new ForbiddenException("Viewers can only read this project.");
    }
}
