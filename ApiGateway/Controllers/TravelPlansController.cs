using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/travel-plans")]
public sealed class TravelPlansController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public TravelPlansController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [Authorize]
    [HttpGet]
    public Task<ActionResult<List<TravelPlanResponseDto>>> List(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.TravelPlans.ListTravelPlansAsync(context, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public Task<ActionResult<TravelPlanResponseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.TravelPlans.GetTravelPlanAsync(context, id, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.TravelPlans.CreateTravelPlanAsync(context, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public Task<ActionResult<TravelPlanResponseDto>> Update(
        Guid id,
        [FromBody] UpdateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<TravelPlanResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.TravelPlans.UpdateTravelPlanAsync(context, id, request, cancellationToken));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.TravelPlans.DeleteTravelPlanAsync(context, id, cancellationToken));
    }
}
