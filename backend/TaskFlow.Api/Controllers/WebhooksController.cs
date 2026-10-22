using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Webhooks;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public class WebhooksController(WebhookService webhooks) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/webhooks")]
    public Task<IReadOnlyList<WebhookResponse>> List(Guid projectId, CancellationToken ct) => webhooks.ListAsync(projectId, ct);

    [HttpPost("projects/{projectId:guid}/webhooks")]
    [ProducesResponseType<WebhookResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, CreateWebhookRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(List), new { projectId }, await webhooks.CreateAsync(projectId, request, ct));

    [HttpPut("webhooks/{id:guid}")]
    public Task<WebhookResponse> Update(Guid id, UpdateWebhookRequest request, CancellationToken ct) => webhooks.UpdateAsync(id, request, ct);

    [HttpPost("webhooks/{id:guid}/rotate-secret")]
    public Task<WebhookResponse> RotateSecret(Guid id, CancellationToken ct) => webhooks.RotateSecretAsync(id, ct);

    [HttpDelete("webhooks/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await webhooks.DeleteAsync(id, ct);

        return NoContent();
    }

    [HttpPost("webhooks/{id:guid}/ping")]
    [ProducesResponseType<WebhookDeliveryResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Ping(Guid id, CancellationToken ct) => Accepted(await webhooks.PingAsync(id, ct));

    [HttpGet("webhooks/{id:guid}/deliveries")]
    public Task<IReadOnlyList<WebhookDeliveryResponse>> Deliveries(Guid id, CancellationToken ct) => webhooks.DeliveriesAsync(id, ct);

    [HttpPost("webhook-deliveries/{id:guid}/redeliver")]
    [ProducesResponseType<WebhookDeliveryResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Redeliver(Guid id, CancellationToken ct) => Accepted(await webhooks.RedeliverAsync(id, ct));
}
