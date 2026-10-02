using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Spaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/spaces")]
public class SpacesController(SpaceService spaces) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SpaceSummaryResponse>> List(CancellationToken ct) => spaces.ListAsync(ct: ct);

    // Galeria de modelos usada na criação de um espaço.
    [HttpGet("templates")]
    public IReadOnlyList<SpaceTemplateResponse> Templates() => spaces.ListTemplates();

    [HttpGet("{id:guid}")]
    public Task<SpaceResponse> Get(Guid id, CancellationToken ct) => spaces.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<SpaceResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateSpaceRequest request, CancellationToken ct)
    {
        var space = await spaces.CreateAsync(request, ct);

        return CreatedAtAction(nameof(Get), new { id = space.Id }, space);
    }

    [HttpPut("{id:guid}")]
    public Task<SpaceResponse> Update(Guid id, UpdateSpaceRequest request, CancellationToken ct) =>
        spaces.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await spaces.DeleteAsync(id, ct);

        return NoContent();
    }
}
