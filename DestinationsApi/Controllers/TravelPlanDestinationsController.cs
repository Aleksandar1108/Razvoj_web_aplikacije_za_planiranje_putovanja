using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DestinationsApi.Dtos;
using DestinationsApi.Infrastructure;
using DestinationsApi.Services;

namespace DestinationsApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/destinations")]
public sealed class TravelPlanDestinationsController : ControllerBase
{
    private readonly IDestinationService _destinations;
    private readonly ITravelPlanAccessGuard _access;

    public TravelPlanDestinationsController(IDestinationService destinations, ITravelPlanAccessGuard access)
    {
        _destinations = destinations;
        _access = access;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelDestinationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelDestinationResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
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
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _destinations.GetAsync(travelPlanId, destinationId, cancellationToken);
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
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        try
        {
            var created = await _destinations.CreateAsync(travelPlanId, request, cancellationToken);
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
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        try
        {
            var updated = await _destinations.UpdateAsync(travelPlanId, destinationId, request, cancellationToken);
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
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();
        var ok = await _destinations.DeleteAsync(travelPlanId, destinationId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
