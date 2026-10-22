using TaskFlow.Infrastructure.Webhooks;

namespace TaskFlow.Api;

// Roda o despachante de webhooks em segundo plano: esvazia a fila e espera o próximo ciclo.
public class WebhookWorker(WebhookDispatcher dispatcher, ILogger<WebhookWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!dispatcher.Options.DispatcherEnabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(dispatcher.Options.PollSeconds));
        do
        {
            try
            {
                while (await dispatcher.DispatchDueAsync(stoppingToken) > 0)
                {
                }
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                // Banco fora do ar etc.: tenta de novo no próximo ciclo; a fila não perde nada.
                logger.LogError(e, "WebhookDispatchFailed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
