using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/members")]
public class ProjectMembersController(ProjectMemberService members) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProjectMemberResponse>> List(Guid projectId, CancellationToken ct) =>
        members.ListAsync(projectId, ct);

    [HttpPost]
    [ProducesResponseType<ProjectMemberResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(Guid projectId, AddMemberRequest request, CancellationToken ct)
    {
        var member = await members.AddAsync(projectId, request, ct);

        return CreatedAtAction(nameof(List), new { projectId }, member);
    }

    [HttpPatch("{userId:guid}")]
    public Task<ProjectMemberResponse> ChangeRole(Guid projectId, Guid userId, ChangeMemberRoleRequest request, CancellationToken ct) =>
        members.ChangeRoleAsync(projectId, userId, request, ct);

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid projectId, Guid userId, CancellationToken ct)
    {
        await members.RemoveAsync(projectId, userId, ct);

        return NoContent();
    }
}
