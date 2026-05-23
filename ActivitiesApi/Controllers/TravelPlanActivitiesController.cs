using ActivitiesApi.Dtos;
using ActivitiesApi.Infrastructure;
using ActivitiesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivitiesApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/activities")]
public sealed class TravelPlanActivitiesController : ControllerBase
{
    private readonly IActivityService _activities;
    private readonly ITravelPlanAccessGuard _access;

    public TravelPlanActivitiesController(IActivityService activities, ITravelPlanAccessGuard access)
    {
        _activities = activities;
        _access = access;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelActivityResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelActivityResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var list = await _activities.ListByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{activityId:guid}")]
    [ProducesResponseType(typeof(TravelActivityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelActivityResponseDto>> Get(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _activities.GetAsync(travelPlanId, activityId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TravelActivityResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelActivityResponseDto>> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        try
        {
            var created = await _activities.CreateAsync(travelPlanId, request, cancellationToken);
            return CreatedAtAction(
                nameof(Get),
                new { travelPlanId, activityId = created.Id },
                created);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{activityId:guid}")]
    [ProducesResponseType(typeof(TravelActivityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelActivityResponseDto>> Update(
        Guid travelPlanId,
        Guid activityId,
        [FromBody] UpdateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        try
        {
            var updated = await _activities.UpdateAsync(travelPlanId, activityId, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{activityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();
        var ok = await _activities.DeleteAsync(travelPlanId, activityId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
