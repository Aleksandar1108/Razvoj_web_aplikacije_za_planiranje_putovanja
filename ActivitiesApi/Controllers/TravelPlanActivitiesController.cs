using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ActivitiesApi.Dtos;
using ActivitiesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivitiesApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/travel-plans/{travelPlanId:guid}/activities")]
public sealed class TravelPlanActivitiesController : ControllerBase
{
    private readonly IActivityService _activities;

    public TravelPlanActivitiesController(IActivityService activities)
    {
        _activities = activities;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelActivityResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelActivityResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        if (await _activities.GetOwnedPlanAsync(userId, travelPlanId, cancellationToken) is null)
            return NotFound();
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var dto = await _activities.GetAsync(userId, travelPlanId, activityId, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _activities.CreateAsync(userId, travelPlanId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _activities.UpdateAsync(userId, travelPlanId, activityId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var ok = await _activities.DeleteAsync(userId, travelPlanId, activityId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
