using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Dashboard;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<DashboardResponse> Get(CancellationToken ct) => dashboard.GetAsync(ct);
}
