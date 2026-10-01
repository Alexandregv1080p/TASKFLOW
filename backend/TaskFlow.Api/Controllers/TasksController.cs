using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/tasks")]
    public Task<IReadOnlyList<TaskResponse>> List(Guid projectId, CancellationToken ct) =>
        tasks.ListByProjectAsync(projectId, ct);

    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        var task = await tasks.CreateAsync(projectId, request, ct);

        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpGet("tasks/{id:guid}")]
    public Task<TaskResponse> Get(Guid id, CancellationToken ct) => tasks.GetAsync(id, ct);

    [HttpGet("tasks/{id:guid}/history")]
    public Task<IReadOnlyList<TaskEventResponse>> History(Guid id, CancellationToken ct) => tasks.HistoryAsync(id, ct);

    [HttpPut("tasks/{id:guid}")]
    public Task<TaskResponse> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        tasks.UpdateAsync(id, request, ct);

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tasks.DeleteAsync(id, ct);

        return NoContent();
    }

    [HttpPatch("tasks/{id:guid}/status")]
    public Task<TaskResponse> ChangeStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken ct) =>
        tasks.ChangeStatusAsync(id, request, ct);

    [HttpPatch("tasks/{id:guid}/assignee")]
    public Task<TaskResponse> Assign(Guid id, AssignTaskRequest request, CancellationToken ct) =>
        tasks.AssignAsync(id, request, ct);
}
