using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/checklist-items")]
public sealed class TravelPlanChecklistController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public TravelPlanChecklistController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<ChecklistItemResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.ListChecklistItemsAsync(context, travelPlanId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid travelPlanId,
        [FromBody] CreateChecklistItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.CreateChecklistItemAsync(context, travelPlanId, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return CreatedAtAction(
            nameof(Get),
            new { travelPlanId, itemId = result.Value!.Id },
            result.Value);
    }

    [HttpGet("{itemId:guid}")]
    public Task<ActionResult<ChecklistItemResponseDto>> Get(
        Guid travelPlanId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.GetChecklistItemAsync(context, travelPlanId, itemId, cancellationToken));
    }

    [HttpPut("{itemId:guid}")]
    public Task<ActionResult<ChecklistItemResponseDto>> Update(
        Guid travelPlanId,
        Guid itemId,
        [FromBody] UpdateChecklistItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<ChecklistItemResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.UpdateChecklistItemAsync(context, travelPlanId, itemId, request, cancellationToken));
    }

    [HttpPatch("{itemId:guid}/toggle")]
    public Task<ActionResult<ChecklistItemResponseDto>> Toggle(
        Guid travelPlanId,
        Guid itemId,
        [FromBody] ToggleChecklistItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.ToggleChecklistItemAsync(context, travelPlanId, itemId, request, cancellationToken));
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Checklist.DeleteChecklistItemAsync(context, travelPlanId, itemId, cancellationToken));
    }
}
