using TaskFlow.Application.Common;

namespace TaskFlow.Infrastructure.Storage;

// Anexos numa pasta do disco (no Docker, um volume em /app/data). Serve para uma instância da API.
// ponytail: com várias réplicas, trocar por armazenamento de objetos (S3, Azure Blob, MinIO) implementando IFileStorage.
public class LocalFileStorage(string root) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(root);

    public async Task SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, ct);
    }

    public Stream OpenRead(string key) => new FileStream(PathFor(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    public Task DeleteAsync(string key)
    {
        File.Delete(PathFor(key));
        return Task.CompletedTask;
    }

    public Task DeletePrefixAsync(string prefix)
    {
        var path = PathFor(prefix);
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        return Task.CompletedTask;
    }

    // As chaves são geradas pelo servidor, mas a checagem fica aqui também: nada fora da pasta raiz.
    private string PathFor(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));

        return path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? path
            : throw new InvalidOperationException("Storage key outside of the storage root.");
    }
}
