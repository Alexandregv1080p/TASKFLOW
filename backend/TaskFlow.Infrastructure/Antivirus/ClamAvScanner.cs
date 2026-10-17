using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using TaskFlow.Application.Common;

namespace TaskFlow.Infrastructure.Antivirus;

// Seção "Antivirus" da configuração. Host vazio = sem antivírus.
public record AntivirusOptions
{
    public string? Host { get; init; }
    public int Port { get; init; } = 3310;
    public int TimeoutSeconds { get; init; } = 30;
}

// Cliente do clamd (daemon do ClamAV) pelo comando INSTREAM: o arquivo vai em blocos "tamanho (4 bytes, big-endian)
// + dados", terminando com um bloco de tamanho zero; a resposta é "stream: OK" ou "stream: <ameaça> FOUND".
// Uma conexão por verificação, sem estado: simples e suficiente para o volume de anexos.
public class ClamAvScanner(AntivirusOptions options) : IVirusScanner
{
    private const int ChunkSize = 64 * 1024;

    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        string reply;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(options.Host!, options.Port, timeout.Token);
            await using var stream = client.GetStream();

            // "z" no início: comando e resposta terminados por \0.
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);

            var buffer = new byte[ChunkSize];
            var length = new byte[4];
            int read;
            while ((read = await content.ReadAsync(buffer, timeout.Token)) > 0)
            {
                BinaryPrimitives.WriteInt32BigEndian(length, read);
                await stream.WriteAsync(length, timeout.Token);
                await stream.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
            }

            BinaryPrimitives.WriteInt32BigEndian(length, 0);
            await stream.WriteAsync(length, timeout.Token);

            reply = await ReadReplyAsync(stream, timeout.Token);
        }
        catch (Exception e) when (e is SocketException or IOException || (e is OperationCanceledException && !ct.IsCancellationRequested))
        {
            throw new ServiceUnavailableException("The antivirus is unavailable right now, so the file was not accepted. Try again in a moment.");
        }

        // "stream: OK" | "stream: Eicar-Test-Signature FOUND" | "INSTREAM size limit exceeded. ERROR" | ...
        if (reply.EndsWith(" OK", StringComparison.Ordinal))
            return ScanResult.Ok;

        if (reply.EndsWith(" FOUND", StringComparison.Ordinal))
            return new ScanResult(false, reply["stream: ".Length..^" FOUND".Length].Trim());

        throw new ServiceUnavailableException($"The antivirus could not scan the file ({reply}).");
    }

    private static async Task<string> ReadReplyAsync(NetworkStream stream, CancellationToken ct)
    {
        var reply = new List<byte>();
        var one = new byte[1];

        while (await stream.ReadAsync(one, ct) == 1 && one[0] != 0)
            reply.Add(one[0]);

        return Encoding.ASCII.GetString(reply.ToArray()).Trim();
    }
}
