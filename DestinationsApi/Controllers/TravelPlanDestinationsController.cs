using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DestinationsApi.Dtos;
using DestinationsApi.Services;

namespace DestinationsApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/travel-plans/{travelPlanId:guid}/destinations")]
public sealed class TravelPlanDestinationsController : ControllerBase
{
    private readonly IDestinationService _destinations;

    public TravelPlanDestinationsController(IDestinationService destinations)
    {
        _destinations = destinations;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelDestinationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelDestinationResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        if (await _destinations.GetOwnedPlanAsync(userId, travelPlanId, cancellationToken) is null)
            return NotFound();
        var list = await _destinations.ListByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{destinationId:guid}")]
    [ProducesResponseType(typeof(TravelDestinationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelDestinationResponseDto>> Get(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var dto = await _destinations.GetAsync(userId, travelPlanId, destinationId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TravelDestinationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelDestinationResponseDto>> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _destinations.CreateAsync(userId, travelPlanId, request, cancellationToken);
            return CreatedAtAction(
                nameof(Get),
                new { travelPlanId, destinationId = created.Id },
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

    [HttpPut("{destinationId:guid}")]
    [ProducesResponseType(typeof(TravelDestinationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelDestinationResponseDto>> Update(
        Guid travelPlanId,
        Guid destinationId,
        [FromBody] UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _destinations.UpdateAsync(userId, travelPlanId, destinationId, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{destinationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var ok = await _destinations.DeleteAsync(userId, travelPlanId, destinationId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
