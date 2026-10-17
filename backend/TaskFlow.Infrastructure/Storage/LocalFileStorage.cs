using TaskFlow.Application.Common;

namespace TaskFlow.Infrastructure.Storage;

// Anexos numa pasta do disco (no Docker, um volume em /app/data). Serve para uma instância da API.
// Com várias réplicas, usar o S3FileStorage (Storage:Provider = S3).
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

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);

        // Pasta inexistente daria DirectoryNotFoundException: o contrato é FileNotFoundException para "não existe".
        return File.Exists(path)
            ? Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            : throw new FileNotFoundException("Attachment file not found.", key);
    }

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
