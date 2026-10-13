using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Notifications;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<NotificationListResponse> List(CancellationToken ct) => notifications.ListAsync(ct);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(id, ct);

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(ct);

        return NoContent();
    }
}
