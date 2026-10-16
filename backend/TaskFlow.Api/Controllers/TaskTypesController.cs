using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public class TaskTypesController(TaskTypeService types) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/task-types")]
    public Task<IReadOnlyList<TaskTypeResponse>> List(Guid projectId, CancellationToken ct) => types.ListAsync(projectId, ct);

    [HttpPost("projects/{projectId:guid}/task-types")]
    [ProducesResponseType<TaskTypeResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, TaskTypeRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(List), new { projectId }, await types.CreateAsync(projectId, request, ct));

    [HttpPut("task-types/{id:guid}")]
    public Task<TaskTypeResponse> Update(Guid id, TaskTypeRequest request, CancellationToken ct) => types.UpdateAsync(id, request, ct);

    [HttpDelete("task-types/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await types.DeleteAsync(id, ct);

        return NoContent();
    }
}
