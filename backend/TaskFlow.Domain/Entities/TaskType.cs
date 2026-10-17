using System.Text.RegularExpressions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Tipo da tarefa (o motivo de ela existir: bug, integração, desenvolvimento...). Cada projeto tem os seus,
// e o dono do projeto cria quantos quiser.
public partial class TaskType : Entity
{
    public const int NameMaxLength = 40;

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }

    // Cor do selo no card, em hexadecimal (#RRGGBB).
    public string Color { get; private set; }

    public TaskType(Guid projectId, string name, string color)
    {
        ProjectId = Guard.NotEmpty(projectId, nameof(ProjectId));
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Color = ValidColor(color);
    }

    // Usado pelo EF Core ao materializar do banco.
    private TaskType()
    {
        Name = null!;
        Color = null!;
    }

    public void Update(string name, string color)
    {
        Name = Guard.Required(name, NameMaxLength, nameof(Name));
        Color = ValidColor(color);
    }

    // Todo projeto novo já nasce com estes; o dono renomeia, apaga ou cria outros.
    // A lista é ordenada por CreatedAt: criados no mesmo instante, o relógio (resolução de até ~15 ms no Windows)
    // daria datas iguais e ordem aleatória. Por isso cada um ganha 1 µs a mais que o anterior.
    public static IEnumerable<TaskType> Defaults(Guid projectId)
    {
        var now = DateTime.UtcNow;

        return new (string Name, string Color)[] { ("Desenvolvimento", "#1868DB"), ("Bug", "#C9372C"), ("Integração", "#6E5DC6") }
            .Select((d, i) => new TaskType(projectId, d.Name, d.Color) { CreatedAt = now.AddTicks(i * 10) })
            .ToList();
    }

    private static string ValidColor(string? color) =>
        color is not null && HexColor().IsMatch(color)
            ? color.ToUpperInvariant()
            : throw new DomainException("Color must be a hex color like #1868DB.");

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
