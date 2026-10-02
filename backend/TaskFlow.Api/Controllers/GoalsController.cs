using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Goals;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/goals")]
public class GoalsController(GoalService goals) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GoalResponse>> List(CancellationToken ct) => goals.ListAsync(ct);

    [HttpPost]
    [ProducesResponseType<GoalResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(GoalRequest request, CancellationToken ct) =>
        CreatedAtAction(nameof(List), await goals.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public Task<GoalResponse> Update(Guid id, GoalRequest request, CancellationToken ct) => goals.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await goals.DeleteAsync(id, ct);

        return NoContent();
    }
}
