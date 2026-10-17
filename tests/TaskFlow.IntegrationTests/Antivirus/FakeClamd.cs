using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TaskFlow.IntegrationTests.Antivirus;

// Um clamd de mentira, em memória: fala o protocolo INSTREAM e acusa "ameaça" quando o arquivo contém o texto
// de teste EICAR. Serve para testar o cliente e o fluxo de upload sem o ClamAV de verdade (que pesa ~1 GB).
public sealed class FakeClamd : IAsyncDisposable
{
    public const string Eicar = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;

    public FakeClamd()
    {
        _listener.Start();
        _loop = Task.Run(AcceptLoopAsync);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    // O que o último cliente mandou, remontado a partir dos blocos (para conferir o enquadramento).
    public byte[] LastReceived { get; private set; } = [];

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using (client)
            {
                var stream = client.GetStream();
                var command = new byte["zINSTREAM\0".Length];
                await stream.ReadExactlyAsync(command);

                var data = new MemoryStream();
                var length = new byte[4];
                while (true)
                {
                    await stream.ReadExactlyAsync(length);
                    var size = BinaryPrimitives.ReadInt32BigEndian(length);
                    if (size == 0)
                        break;

                    var chunk = new byte[size];
                    await stream.ReadExactlyAsync(chunk);
                    data.Write(chunk);
                }

                LastReceived = data.ToArray();
                var infected = Encoding.ASCII.GetString(LastReceived).Contains("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");
                await stream.WriteAsync(Encoding.ASCII.GetBytes(infected ? "stream: Eicar-Test-Signature FOUND\0" : "stream: OK\0"));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _loop;
    }
}
