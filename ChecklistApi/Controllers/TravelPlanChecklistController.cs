using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChecklistApi.Dtos;
using ChecklistApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/travel-plans/{travelPlanId:guid}/checklist")]
public sealed class TravelPlanChecklistController : ControllerBase
{
    private readonly IChecklistService _checklist;

    public TravelPlanChecklistController(IChecklistService checklist)
    {
        _checklist = checklist;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChecklistItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemResponseDto>>> List(Guid travelPlanId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        if (await _checklist.GetOwnedPlanAsync(userId, travelPlanId, cancellationToken) is null)
            return NotFound();
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _checklist.CreateAsync(userId, travelPlanId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var dto = await _checklist.GetAsync(userId, travelPlanId, itemId, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _checklist.UpdateAsync(userId, travelPlanId, itemId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var updated = await _checklist.ToggleAsync(userId, travelPlanId, itemId, request.IsDone, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var ok = await _checklist.DeleteAsync(userId, travelPlanId, itemId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
