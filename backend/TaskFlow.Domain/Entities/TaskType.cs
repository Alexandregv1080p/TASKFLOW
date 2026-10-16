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
    public static IEnumerable<TaskType> Defaults(Guid projectId) =>
    [
        new(projectId, "Desenvolvimento", "#1868DB"),
        new(projectId, "Bug", "#C9372C"),
        new(projectId, "Integração", "#6E5DC6"),
    ];

    private static string ValidColor(string? color) =>
        color is not null && HexColor().IsMatch(color)
            ? color.ToUpperInvariant()
            : throw new DomainException("Color must be a hex color like #1868DB.");

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
