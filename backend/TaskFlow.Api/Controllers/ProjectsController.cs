using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(ProjectService projects, ProjectExportService export) : ControllerBase
{
    // Diretório de projetos. scope: All (padrão), Mine ou Archived; search: trecho do nome; spaceId: só de um espaço.
    [HttpGet]
    public Task<IReadOnlyList<ProjectSummaryResponse>> List(
        [FromQuery] ProjectScope scope = ProjectScope.All,
        [FromQuery] string? search = null,
        [FromQuery] Guid? spaceId = null,
        CancellationToken ct = default) =>
        projects.ListAsync(scope, search, spaceId: spaceId, ct: ct);

    // Tarefas do projeto em CSV (planilha). Quem está no projeto exporta, inclusive o visualizador.
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        var file = await export.TasksCsvAsync(id, ct);

        return File(file.Content, "text/csv; charset=utf-8", file.FileName);
    }

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

    [HttpPatch("{id:guid}/status")]
    public Task<ProjectResponse> ChangeStatus(Guid id, ChangeProjectStatusRequest request, CancellationToken ct) =>
        projects.ChangeStatusAsync(id, request, ct);

    [HttpPost("{id:guid}/archive")]
    public Task<ProjectResponse> Archive(Guid id, CancellationToken ct) => projects.ArchiveAsync(id, ct);

    [HttpPost("{id:guid}/restore")]
    public Task<ProjectResponse> Restore(Guid id, CancellationToken ct) => projects.RestoreAsync(id, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projects.DeleteAsync(id, ct);

        return NoContent();
    }
}
