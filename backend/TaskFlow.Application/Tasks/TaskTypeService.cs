using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks;

public record TaskTypeRequest(string Name, string Color);

public record TaskTypeResponse(Guid Id, string Name, string Color)
{
    public static TaskTypeResponse From(TaskType t) => new(t.Id, t.Name, t.Color);
}

// Tipos de tarefa de um projeto: quem está no projeto vê; só o dono cria, edita e exclui.
public class TaskTypeService(ITaskFlowDbContext db, ICurrentUser currentUser, ILiveNotifier? live = null)
{
    public async Task<IReadOnlyList<TaskTypeResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        var types = await db.TaskTypes.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        return types.Select(TaskTypeResponse.From).ToList();
    }

    public async Task<TaskTypeResponse> CreateAsync(Guid projectId, TaskTypeRequest request, CancellationToken ct = default)
    {
        var project = await db.FindOwnedProjectAsync(currentUser.UserId, projectId, ct);
        project.EnsureActive();

        var type = new TaskType(project.Id, request.Name, request.Color);
        await EnsureUniqueNameAsync(project.Id, type.Name, null, ct);

        db.TaskTypes.Add(type);
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(project.Id);

        return TaskTypeResponse.From(type);
    }

    public async Task<TaskTypeResponse> UpdateAsync(Guid id, TaskTypeRequest request, CancellationToken ct = default)
    {
        var type = await FindOwnedAsync(id, ct);

        type.Update(request.Name, request.Color);
        await EnsureUniqueNameAsync(type.ProjectId, type.Name, type.Id, ct);

        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(type.ProjectId);

        return TaskTypeResponse.From(type);
    }

    // As tarefas desse tipo ficam sem tipo (FK com SET NULL); nada é apagado além do tipo.
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var type = await FindOwnedAsync(id, ct);

        db.TaskTypes.Remove(type);
        await db.SaveChangesAsync(ct);
        await live.ChangedAsync(type.ProjectId);
    }

    // Tipo de projeto sem acesso: 404. Membro tentando editar: 403 (a regra do ProjectAccess).
    private async Task<TaskType> FindOwnedAsync(Guid id, CancellationToken ct)
    {
        var type = await db.TaskTypes.SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(TaskType), id);

        var project = await db.FindOwnedProjectAsync(currentUser.UserId, type.ProjectId, ct);
        project.EnsureActive();

        return type;
    }

    private async Task EnsureUniqueNameAsync(Guid projectId, string name, Guid? exceptId, CancellationToken ct)
    {
        var lower = name.ToLower();

        if (await db.TaskTypes.AnyAsync(t => t.ProjectId == projectId && t.Id != exceptId && t.Name.ToLower() == lower, ct))
            throw new ConflictException($"This project already has a task type named '{name}'.");
    }
}
