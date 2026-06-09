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

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.CreateAdminUserAsync(context, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return Created($"/api/v1/admin/users/{result.Value!.Id}", result.Value);
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

    [HttpDelete("users/{userId:guid}")]
    public Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.DeleteAdminUserAsync(context, userId, cancellationToken));
    }
}
