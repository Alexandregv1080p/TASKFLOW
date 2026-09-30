using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(ProjectService projects) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProjectSummaryResponse>> List(CancellationToken ct) => projects.ListAsync(ct: ct);

    [HttpGet("{id:guid}")]
    public Task<ProjectResponse> Get(Guid id, CancellationToken ct) => projects.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var project = await projects.CreateAsync(request, ct);

        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public Task<ProjectResponse> Update(Guid id, UpdateProjectRequest request, CancellationToken ct) =>
        projects.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projects.DeleteAsync(id, ct);

        return NoContent();
    }
}
