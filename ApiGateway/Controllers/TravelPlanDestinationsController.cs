using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/destinations")]
public sealed class TravelPlanDestinationsController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public TravelPlanDestinationsController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<TravelDestinationResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.ListDestinationsAsync(context, travelPlanId, cancellationToken));
    }

    [HttpGet("{destinationId:guid}")]
    public Task<ActionResult<TravelDestinationResponseDto>> Get(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.GetDestinationAsync(context, travelPlanId, destinationId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.CreateDestinationAsync(context, travelPlanId, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return CreatedAtAction(
            nameof(Get),
            new { travelPlanId, destinationId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{destinationId:guid}")]
    public Task<ActionResult<TravelDestinationResponseDto>> Update(
        Guid travelPlanId,
        Guid destinationId,
        [FromBody] UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<TravelDestinationResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.UpdateDestinationAsync(context, travelPlanId, destinationId, request, cancellationToken));
    }

    [HttpDelete("{destinationId:guid}")]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.DeleteDestinationAsync(context, travelPlanId, destinationId, cancellationToken));
    }
}
