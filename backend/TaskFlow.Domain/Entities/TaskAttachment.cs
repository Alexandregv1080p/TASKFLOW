using TaskFlow.Domain.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

// Arquivo anexado a uma tarefa. O conteúdo fica no armazenamento de arquivos (StorageKey); aqui só os metadados.
public class TaskAttachment : Entity
{
    public const int FileNameMaxLength = 200;
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    // Lista do que é aceito, pela extensão. O tipo de conteúdo vem daqui, nunca do que o navegador declarou.
    // Nada que o navegador execute (html, svg, js) nem executáveis.
    public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".log"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",
        [".json"] = "application/json",
        [".zip"] = "application/zip",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    public Guid TaskId { get; private set; }
    public Guid? UploadedById { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    // Caminho no armazenamento, gerado pelo servidor: projeto/tarefa/anexo. O nome enviado nunca vira caminho.
    public string StorageKey { get; private set; }

    public TaskAttachment(Guid projectId, Guid taskId, Guid uploadedById, string fileName, long sizeBytes)
    {
        TaskId = Guard.NotEmpty(taskId, nameof(TaskId));
        UploadedById = Guard.NotEmpty(uploadedById, nameof(UploadedById));

        // Só o nome, sem pastas que o navegador possa ter mandado ("C:\fakepath\..."), e sem caracteres de controle.
        var name = Path.GetFileName(fileName?.Replace('\\', '/') ?? "");
        name = new string(name.Where(c => !char.IsControl(c)).ToArray());
        FileName = Guard.Required(name, FileNameMaxLength, nameof(FileName));

        ContentType = AllowedTypes.TryGetValue(Path.GetExtension(FileName).ToLowerInvariant(), out var type)
            ? type
            : throw new DomainException("This file type is not allowed. Use images, PDF, text, CSV, JSON, ZIP or Office documents.");

        if (sizeBytes <= 0)
            throw new DomainException("The file is empty.");
        if (sizeBytes > MaxSizeBytes)
            throw new DomainException("The file is larger than 10 MB.");
        SizeBytes = sizeBytes;

        StorageKey = $"{Guard.NotEmpty(projectId, "ProjectId")}/{TaskId}/{Id}";
    }

    // Usado pelo EF Core ao materializar do banco.
    private TaskAttachment()
    {
        FileName = null!;
        ContentType = null!;
        StorageKey = null!;
    }
}
