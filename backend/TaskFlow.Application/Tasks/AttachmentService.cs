using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Tasks;

public record AttachmentResponse(Guid Id, string FileName, string ContentType, long SizeBytes, Guid? UploadedById, DateTime CreatedAt)
{
    public static AttachmentResponse From(TaskAttachment a) => new(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedById, a.CreatedAt);
}

public record AttachmentDownload(Stream Content, string FileName, string ContentType);

// Anexos das tarefas. Ver e baixar: quem está no projeto. Anexar: quem pode editar (não visualizador, projeto ativo).
// Excluir: quem anexou ou o dono do projeto.
public class AttachmentService(
    ITaskFlowDbContext db,
    ICurrentUser currentUser,
    IFileStorage files,
    ILogger<AttachmentService> logger,
    ILiveNotifier? live = null)
{
    public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await FindTaskAsync(taskId, ct);

        var attachments = await db.TaskAttachments.AsNoTracking()
            .Where(a => a.TaskId == task.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return attachments.Select(AttachmentResponse.From).ToList();
    }

    public async Task<AttachmentResponse> UploadAsync(Guid taskId, string fileName, long size, Stream content, CancellationToken ct = default)
    {
        var task = await FindTaskAsync(taskId, ct);
        await EnsureCanEditAsync(task.ProjectId, ct);

        // Valida nome, tipo e tamanho antes de gravar qualquer coisa no disco.
        var attachment = new TaskAttachment(task.ProjectId, task.Id, currentUser.UserId, fileName, size);

        await files.SaveAsync(attachment.StorageKey, content, ct);

        try
        {
            db.TaskAttachments.Add(attachment);
            db.TaskEvents.Add(new TaskEvent(task.Id, currentUser.UserId, TaskEventType.AttachmentAdded, null, Truncate(attachment.FileName)));
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Não gravou no banco: o arquivo não pode ficar órfão no disco.
            await files.TryDeleteAsync(f => f.DeleteAsync(attachment.StorageKey));
            throw;
        }

        logger.LogInformation(
            "AttachmentAdded {AttachmentId} {TaskId} {SizeBytes} {UserId}", attachment.Id, task.Id, attachment.SizeBytes, currentUser.UserId);
        await live.ChangedAsync(task.ProjectId, task.Id);

        return AttachmentResponse.From(attachment);
    }

    public async Task<AttachmentDownload> DownloadAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await FindAsync(id, ct);

        return new AttachmentDownload(await files.OpenReadAsync(attachment.StorageKey, ct), attachment.FileName, attachment.ContentType);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await FindAsync(id, ct);
        var task = await db.Tasks.SingleAsync(t => t.Id == attachment.TaskId, ct);
        await EnsureCanEditAsync(task.ProjectId, ct);

        var isOwner = await db.Projects.AnyAsync(p => p.Id == task.ProjectId && p.OwnerId == currentUser.UserId, ct);
        if (attachment.UploadedById != currentUser.UserId && !isOwner)
            throw new ForbiddenException("Only who uploaded the file or the project owner can delete it.");

        db.TaskAttachments.Remove(attachment);
        db.TaskEvents.Add(new TaskEvent(task.Id, currentUser.UserId, TaskEventType.AttachmentRemoved, Truncate(attachment.FileName)));
        await db.SaveChangesAsync(ct);

        await files.TryDeleteAsync(f => f.DeleteAsync(attachment.StorageKey));
        logger.LogInformation("AttachmentRemoved {AttachmentId} {TaskId} {UserId}", attachment.Id, task.Id, currentUser.UserId);
        await live.ChangedAsync(task.ProjectId, task.Id);
    }

    private static string Truncate(string value) =>
        value.Length <= TaskEvent.ValueMaxLength ? value : value[..TaskEvent.ValueMaxLength];

    // Tarefa de projeto sem acesso: 404, como no resto da API.
    private async Task<TaskItem> FindTaskAsync(Guid taskId, CancellationToken ct)
    {
        var projects = db.AccessibleProjects(currentUser.UserId);

        return await db.Tasks.SingleOrDefaultAsync(t => t.Id == taskId && projects.Any(p => p.Id == t.ProjectId), ct)
            ?? throw new NotFoundException(nameof(TaskItem), taskId);
    }

    private async Task<TaskAttachment> FindAsync(Guid id, CancellationToken ct)
    {
        var projects = db.AccessibleProjects(currentUser.UserId);

        return await db.TaskAttachments.SingleOrDefaultAsync(
                a => a.Id == id && db.Tasks.Any(t => t.Id == a.TaskId && projects.Any(p => p.Id == t.ProjectId)), ct)
            ?? throw new NotFoundException(nameof(TaskAttachment), id);
    }

    // Mesma regra das outras alterações: projeto ativo e quem pode editar (visualizador não anexa).
    private async Task EnsureCanEditAsync(Guid projectId, CancellationToken ct)
    {
        if (await db.Projects.AnyAsync(p => p.Id == projectId && p.ArchivedAt != null, ct))
            throw new DomainException("This project is archived. Restore it to make changes.");

        if (!await db.CanEditProjectAsync(projectId, currentUser.UserId, ct))
            throw new ForbiddenException("Viewers can only read this project.");
    }
}
