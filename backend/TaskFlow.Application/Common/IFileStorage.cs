namespace TaskFlow.Application.Common;

// Onde ficam os arquivos anexados. Hoje é uma pasta no disco (LocalFileStorage); trocar por S3/Azure Blob é
// implementar esta interface. As chaves são geradas pelo servidor ("projeto/tarefa/anexo").
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken ct = default);

    Stream OpenRead(string key);

    Task DeleteAsync(string key);

    // Apaga tudo o que começa com o prefixo (os arquivos de uma tarefa ou de um projeto inteiro).
    Task DeletePrefixAsync(string prefix);
}

public static class FileStorageExtensions
{
    // A exclusão no banco já foi gravada: falhar ao apagar o arquivo não pode desfazer nem virar erro para quem excluiu.
    // ponytail: o que falhar aqui fica órfão no disco; com volume real, um job periódico compara disco e banco.
    public static async Task TryDeleteAsync(this IFileStorage? files, Func<IFileStorage, Task> delete)
    {
        if (files is null)
            return;

        try
        {
            await delete(files);
        }
        catch (IOException)
        {
        }
    }
}
