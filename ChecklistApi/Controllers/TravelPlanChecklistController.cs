using ChecklistApi.Dtos;
using ChecklistApi.Infrastructure;
using ChecklistApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/checklist-items")]
public sealed class TravelPlanChecklistController : ControllerBase
{
    private readonly IChecklistService _checklist;
    private readonly ITravelPlanAccessGuard _access;

    public TravelPlanChecklistController(IChecklistService checklist, ITravelPlanAccessGuard access)
    {
        _checklist = checklist;
        _access = access;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChecklistItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemResponseDto>>> List(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var list = await _checklist.ListByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChecklistItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemResponseDto>> Create(
        Guid travelPlanId,
        [FromBody] CreateChecklistItemRequestDto request,
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
            var created = await _checklist.CreateAsync(travelPlanId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { travelPlanId, itemId = created.Id }, created);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{itemId:guid}")]
    [ProducesResponseType(typeof(ChecklistItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemResponseDto>> Get(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _checklist.GetAsync(travelPlanId, itemId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{itemId:guid}")]
    [ProducesResponseType(typeof(ChecklistItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemResponseDto>> Update(
        Guid travelPlanId,
        Guid itemId,
        [FromBody] UpdateChecklistItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        var updated = await _checklist.UpdateAsync(travelPlanId, itemId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{itemId:guid}/toggle")]
    [ProducesResponseType(typeof(ChecklistItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemResponseDto>> Toggle(
        Guid travelPlanId,
        Guid itemId,
        [FromBody] ToggleChecklistItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();
        var updated = await _checklist.ToggleAsync(travelPlanId, itemId, request.IsDone, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();
        var ok = await _checklist.DeleteAsync(travelPlanId, itemId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
