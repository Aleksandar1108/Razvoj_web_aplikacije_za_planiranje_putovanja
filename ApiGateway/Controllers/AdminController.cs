using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public AdminController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet("stats")]
    public Task<ActionResult<AdminSystemStatsDto>> Stats(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.GetAdminStatsAsync(context, cancellationToken));
    }

    [HttpGet("users")]
    public Task<ActionResult<List<AdminUserListItemDto>>> ListUsers(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.ListAdminUsersAsync(context, cancellationToken));
    }

    [HttpGet("users/{userId:guid}")]
    public Task<ActionResult<AdminUserListItemDto>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.GetAdminUserAsync(context, userId, cancellationToken));
    }

    [HttpPatch("users/{userId:guid}")]
    public Task<ActionResult<AdminUserListItemDto>> UpdateUser(
        Guid userId,
        [FromBody] UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<AdminUserListItemDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.UpdateAdminUserAsync(context, userId, request, cancellationToken));
    }
}
