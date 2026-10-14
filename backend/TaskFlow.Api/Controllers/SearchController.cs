using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Search;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(SearchService search) : ControllerBase
{
    [HttpGet]
    public Task<SearchResponse> Search([FromQuery] string? q, CancellationToken ct) => search.SearchAsync(q, ct);
}
