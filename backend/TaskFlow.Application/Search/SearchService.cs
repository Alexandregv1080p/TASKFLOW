using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Search;

public record SearchTaskResult(Guid Id, Guid ProjectId, string ProjectName, int Number, string Title, TaskItemStatus Status);

public record SearchProjectResult(Guid Id, string Name, string? SpaceName, bool Archived);

public record SearchSpaceResult(Guid Id, string Name);

public record SearchResponse(
    IReadOnlyList<SearchTaskResult> Tasks,
    IReadOnlyList<SearchProjectResult> Projects,
    IReadOnlyList<SearchSpaceResult> Spaces);

// Busca global (Ctrl+K): tarefas, projetos e espaços a que o usuário tem acesso, pelo trecho do nome.
// Termo que termina em número ("12", "WEB-12") também acha a tarefa por número; o cliente filtra pela sigla,
// que é derivada do nome do projeto e não fica no banco.
// ponytail: LIKE '%termo%' sem diferenciar maiúsculas (nem acentos) varre as tabelas; com volume real,
// índice trigram (pg_trgm) ou busca full-text do PostgreSQL.
public partial class SearchService(ITaskFlowDbContext db, ICurrentUser currentUser)
{
    public const int MinLength = 2;
    private const int TaskLimit = 8;
    private const int OtherLimit = 5;

    private static readonly SearchResponse Empty = new([], [], []);

    public async Task<SearchResponse> SearchAsync(string? q, CancellationToken ct = default)
    {
        var term = q?.Trim().ToLower() ?? "";
        if (term.Length < MinLength && !TaskNumber().IsMatch(term))
            return Empty;

        var userId = currentUser.UserId;
        var projects = db.AccessibleProjects(userId);
        int? number = TaskNumber().Match(term) is { Success: true } m ? int.Parse(m.Groups[1].Value) : null;

        // Abertas antes das concluídas, depois as mais recentes.
        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => projects.Any(p => p.Id == t.ProjectId)
                && (t.Title.ToLower().Contains(term) || (number != null && t.Number == number)))
            .OrderBy(t => t.Status == TaskItemStatus.Done)
            .ThenByDescending(t => t.CreatedAt)
            .Take(TaskLimit)
            .Select(t => new SearchTaskResult(
                t.Id,
                t.ProjectId,
                db.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).First(),
                t.Number,
                t.Title,
                t.Status))
            .ToListAsync(ct);

        var matchedProjects = await projects.AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term))
            .OrderBy(p => p.ArchivedAt != null)
            .ThenBy(p => p.Name)
            .Take(OtherLimit)
            .Select(p => new SearchProjectResult(
                p.Id,
                p.Name,
                db.Spaces.Where(s => s.Id == p.SpaceId).Select(s => s.Name).FirstOrDefault(),
                p.ArchivedAt != null))
            .ToListAsync(ct);

        var spaces = await db.AccessibleSpaces(userId).AsNoTracking()
            .Where(s => s.Name.ToLower().Contains(term))
            .OrderBy(s => s.Name)
            .Take(OtherLimit)
            .Select(s => new SearchSpaceResult(s.Id, s.Name))
            .ToListAsync(ct);

        return new SearchResponse(tasks, matchedProjects, spaces);
    }

    // "12", "#12" ou "web-12": o número da tarefa no fim do termo.
    [GeneratedRegex(@"^(?:#|[a-z]+-)?(\d{1,9})$")]
    private static partial Regex TaskNumber();
}
