using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class SharingController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public SharingController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpPost("travel-plans/{travelPlanId:guid}/share-links")]
    public async Task<IActionResult> CreateShareLink(
        Guid travelPlanId,
        [FromBody] CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Sharing.CreateShareLinkAsync(context, travelPlanId, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("shared-plans")]
    public Task<ActionResult<List<SharedTravelPlanListItemDto>>> ListSharedPlans(
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Sharing.ListSharedPlansAsync(context, cancellationToken));
    }

    [HttpPost("share-links/claim")]
    public Task<ActionResult<ClaimShareLinkResponseDto>> Claim(
        [FromBody] ClaimShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<ClaimShareLinkResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Sharing.ClaimShareLinkAsync(context, request, cancellationToken));
    }
}
