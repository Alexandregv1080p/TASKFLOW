using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public class AttachmentsController(AttachmentService attachments) : ControllerBase
{
    // Um pouco acima de 10 MB para caber o envelope do multipart; o limite real (10 MB) é regra do domínio.
    private const long RequestLimit = TaskAttachment.MaxSizeBytes + 1024 * 1024;

    [HttpGet("tasks/{id:guid}/attachments")]
    public Task<IReadOnlyList<AttachmentResponse>> List(Guid id, CancellationToken ct) => attachments.ListAsync(id, ct);

    [HttpPost("tasks/{id:guid}/attachments")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [ProducesResponseType<AttachmentResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var attachment = await attachments.UploadAsync(id, file.FileName, file.Length, content, ct);

        return CreatedAtAction(nameof(List), new { id }, attachment);
    }

    // Sempre como download (Content-Disposition: attachment) e sem o navegador adivinhar o tipo (nosniff):
    // um arquivo enviado por alguém nunca é aberto como página do TaskFlow.
    [HttpGet("attachments/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await attachments.DownloadAsync(id, ct);
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await attachments.DeleteAsync(id, ct);

        return NoContent();
    }
}
