using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Users;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserSummaryResponse>> List(CancellationToken ct) => users.ListAsync(ct);
}
