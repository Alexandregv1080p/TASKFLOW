using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects;

public record ExportFile(string FileName, byte[] Content);

// Exporta as tarefas de um projeto em CSV: "meus dados são meus". Qualquer pessoa do projeto exporta,
// inclusive o visualizador (ele já pode ler tudo). Cada exportação fica registrada no log (auditoria).
public class ProjectExportService(ITaskFlowDbContext db, ICurrentUser currentUser, ILogger<ProjectExportService> logger)
{
    // Ponto e vírgula + BOM UTF-8: é o que o Excel em português abre direto, com acentos e colunas certas.
    private const char Separator = ';';

    private static readonly string[] Header =
        ["Chave", "Título", "Tipo", "Status", "Prioridade", "Responsável", "Criador", "Tarefa-pai", "Prazo", "Criada em (UTC)", "Descrição"];

    private static readonly Dictionary<TaskItemStatus, string> StatusLabel = new()
    {
        [TaskItemStatus.Todo] = "A fazer",
        [TaskItemStatus.InProgress] = "Em andamento",
        [TaskItemStatus.Done] = "Concluído",
    };

    private static readonly Dictionary<TaskPriority, string> PriorityLabel = new()
    {
        [TaskPriority.Low] = "Baixa",
        [TaskPriority.Medium] = "Média",
        [TaskPriority.High] = "Alta",
    };

    public async Task<ExportFile> TasksCsvAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await db.FindAccessibleProjectAsync(currentUser.UserId, projectId, ct);

        var tasks = await db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == project.Id)
            .OrderBy(t => t.Number)
            .Select(t => new
            {
                t.Number,
                t.Title,
                Type = db.TaskTypes.Where(x => x.Id == t.TypeId).Select(x => x.Name).FirstOrDefault(),
                t.Status,
                t.Priority,
                Assignee = db.Users.Where(u => u.Id == t.AssignedToId).Select(u => u.Name).FirstOrDefault(),
                Creator = db.Users.Where(u => u.Id == t.CreatedById).Select(u => u.Name).FirstOrDefault(),
                ParentNumber = db.Tasks.Where(p => p.Id == t.ParentId).Select(p => (int?)p.Number).FirstOrDefault(),
                t.DueDate,
                t.CreatedAt,
                t.Description,
            })
            .ToListAsync(ct);

        var csv = new StringBuilder();
        AppendRow(csv, Header);

        foreach (var t in tasks)
        {
            AppendRow(csv,
            [
                ProjectKey.TaskKey(project.Name, t.Number),
                t.Title,
                t.Type,
                StatusLabel[t.Status],
                PriorityLabel[t.Priority],
                t.Assignee,
                t.Creator,
                t.ParentNumber is { } parent ? ProjectKey.TaskKey(project.Name, parent) : null,
                // Cultura invariante: ":" num formato próprio vira o separador de hora da cultura do servidor.
                t.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                t.Description,
            ]);
        }

        logger.LogInformation("ProjectExported {ProjectId} {TaskCount} {UserId}", project.Id, tasks.Count, currentUser.UserId);

        var fileName = $"taskflow-{ProjectKey.For(project.Name).ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return new ExportFile(fileName, [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())]);
    }

    private static void AppendRow(StringBuilder csv, IEnumerable<string?> fields)
    {
        csv.AppendJoin(Separator, fields.Select(Field));
        csv.Append("\r\n");
    }

    // Um campo do CSV: entre aspas quando tem separador, aspas ou quebra de linha (aspas internas dobradas).
    // Texto que começa com = + - @ vira fórmula ao abrir a planilha (injeção de CSV/fórmula): ganha um
    // apóstrofo na frente, que o Excel trata como "isto é texto".
    public static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;

        return value.IndexOfAny([Separator, '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
