using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/activities")]
public sealed class TravelPlanActivitiesController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public TravelPlanActivitiesController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<TravelActivityResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Activities.ListActivitiesAsync(context, travelPlanId, cancellationToken));
    }

    [HttpGet("{activityId:guid}")]
    public Task<ActionResult<TravelActivityResponseDto>> Get(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Activities.GetActivityAsync(context, travelPlanId, activityId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Activities.CreateActivityAsync(context, travelPlanId, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return CreatedAtAction(
            nameof(Get),
            new { travelPlanId, activityId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{activityId:guid}")]
    public Task<ActionResult<TravelActivityResponseDto>> Update(
        Guid travelPlanId,
        Guid activityId,
        [FromBody] UpdateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<TravelActivityResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Activities.UpdateActivityAsync(context, travelPlanId, activityId, request, cancellationToken));
    }

    [HttpDelete("{activityId:guid}")]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Activities.DeleteActivityAsync(context, travelPlanId, activityId, cancellationToken));
    }
}
