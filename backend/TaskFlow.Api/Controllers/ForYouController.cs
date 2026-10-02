using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.ForYou;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/for-you")]
public class ForYouController(ForYouService forYou) : ControllerBase
{
    // filter: Recommended (padrão), Assigned, Starred, Worked ou Viewed.
    [HttpGet]
    public Task<ForYouResponse> Get([FromQuery] ForYouFilter filter = ForYouFilter.Recommended, CancellationToken ct = default) =>
        forYou.GetAsync(filter, ct);
}
