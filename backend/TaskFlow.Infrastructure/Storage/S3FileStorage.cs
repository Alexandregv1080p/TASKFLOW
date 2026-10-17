using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using TaskFlow.Application.Common;

namespace TaskFlow.Infrastructure.Storage;

// Configuração do armazenamento de anexos (seção "Storage"). Provider: "Local" (padrão, pasta no disco) ou "S3".
public record StorageOptions
{
    public string Provider { get; init; } = "Local";
    public string? Path { get; init; }
    public S3Options S3 { get; init; } = new();
}

// ServiceUrl vazio = AWS de verdade (pela região); preenchido = serviço compatível com S3 (MinIO, R2, etc.).
public record S3Options
{
    public string? ServiceUrl { get; init; }
    public string Region { get; init; } = "us-east-1";
    public string Bucket { get; init; } = "taskflow-attachments";
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
}

// Anexos num bucket S3 (ou compatível). Funciona com várias réplicas da API, ao contrário da pasta local.
// O bucket é privado: o download continua passando pela API, que confere quem pode ver o arquivo.
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public S3FileStorage(S3Options options)
    {
        var config = new AmazonS3Config
        {
            // MinIO e afins usam o bucket no caminho da URL, não no subdomínio.
            ForcePathStyle = !string.IsNullOrEmpty(options.ServiceUrl),
            AuthenticationRegion = options.Region,
            // Checksum só quando a operação exige: compatível com MinIO e outros serviços S3.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (string.IsNullOrEmpty(options.ServiceUrl))
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
        else
            config.ServiceURL = options.ServiceUrl;

        // Sem chaves na configuração: usa as credenciais do ambiente (perfil da AWS, papel da instância/contêiner).
        _s3 = string.IsNullOrEmpty(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        _bucket = options.Bucket;
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);
        await _s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, InputStream = content, AutoCloseStream = false }, ct);
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_bucket, key, ct);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException("Attachment file not found.", key);
        }
    }

    public Task DeleteAsync(string key) => _s3.DeleteObjectAsync(_bucket, key);

    // S3 não tem pastas: lista as chaves com o prefixo e apaga em lotes (até 1000 por chamada).
    public async Task DeletePrefixAsync(string prefix)
    {
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix.TrimEnd('/') + "/" };
        ListObjectsV2Response page;

        do
        {
            page = await _s3.ListObjectsV2Async(request);
            var keys = (page.S3Objects ?? []).Select(o => new KeyVersion { Key = o.Key }).ToList();
            if (keys.Count > 0)
                await _s3.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = _bucket, Objects = keys });

            request.ContinuationToken = page.NextContinuationToken;
        }
        while (page.IsTruncated == true);
    }

    // Cria o bucket na primeira gravação, se ainda não existir (útil no MinIO local; na AWS ele já existe).
    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady)
            return;

        await _bucketLock.WaitAsync(ct);
        try
        {
            if (_bucketReady)
                return;

            try
            {
                await _s3.PutBucketAsync(_bucket, ct);
            }
            catch (AmazonS3Exception e) when (e.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    public void Dispose()
    {
        _s3.Dispose();
        _bucketLock.Dispose();
    }
}
