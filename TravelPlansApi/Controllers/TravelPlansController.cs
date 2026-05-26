using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrossService.Access;
using TravelPlansApi.Dtos;
using TravelPlansApi.Services;

namespace TravelPlansApi.Controllers;

[ApiController]
[Route("api/v1/travel-plans")]
public sealed class TravelPlansController : ControllerBase
{
    private readonly ITravelPlanService _plans;
    private readonly ITravelPlanAccessGuard _access;
    private readonly IAdminPlanNotificationService _adminNotifications;

    public TravelPlansController(
        ITravelPlanService plans,
        ITravelPlanAccessGuard access,
        IAdminPlanNotificationService adminNotifications)
    {
        _plans = plans;
        _access = access;
        _adminNotifications = adminNotifications;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelPlanResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TravelPlanResponseDto>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var list = await _plans.ListForUserAsync(userId, cancellationToken);
        return Ok(list);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPlanResponseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, id, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();

        var dto = await _plans.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TravelPlanResponseDto>> Create(
        [FromBody] CreateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _plans.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPlanResponseDto>> Update(
        Guid id,
        [FromBody] UpdateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var access = await _access.ResolveAsync(HttpContext, id, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        var before = access.IsAdminOverride ? await _plans.GetByIdAsync(id, cancellationToken) : null;

        try
        {
            var updated = access.IsAdminOverride
                ? await _plans.UpdateByPlanIdAsync(id, request, cancellationToken)
                : await _plans.UpdateAsync(userId, id, request, cancellationToken);

            if (updated is null)
                return NotFound();

            if (access.IsAdminOverride && before is not null)
                await NotifyPlanUpdateAsync(before, updated, cancellationToken);

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var access = await _access.ResolveAsync(HttpContext, id, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        string? planName = null;
        if (access.IsAdminOverride)
        {
            var before = await _plans.GetByIdAsync(id, cancellationToken);
            planName = before?.Name;
        }

        var ok = access.IsAdminOverride
            ? await _plans.DeleteByPlanIdAsync(id, cancellationToken)
            : await _plans.DeleteAsync(userId, id, cancellationToken);

        if (!ok)
            return NotFound();

        if (access.IsAdminOverride)
        {
            await TryNotifyAdminAsync(
                access,
                id,
                AdminNotificationCategories.PlanBasic,
                AdminMutationAction.Deleted,
                planName,
                null,
                cancellationToken);
        }

        return NoContent();
    }

    private async Task NotifyPlanUpdateAsync(
        TravelPlanResponseDto before,
        TravelPlanResponseDto after,
        CancellationToken cancellationToken)
    {
        var access = new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);
        var notesChanged = !string.Equals(
            before.GeneralNotes?.Trim() ?? string.Empty,
            after.GeneralNotes?.Trim() ?? string.Empty,
            StringComparison.Ordinal);

        var basicChanged =
            before.Name != after.Name
            || before.ShortDescription != after.ShortDescription
            || before.StartDate != after.StartDate
            || before.EndDate != after.EndDate
            || before.PlannedBudget != after.PlannedBudget;

        if (basicChanged)
        {
            await TryNotifyAdminAsync(
                access,
                after.Id,
                AdminNotificationCategories.PlanBasic,
                AdminMutationAction.Updated,
                null,
                null,
                cancellationToken);
        }

        if (notesChanged)
        {
            await TryNotifyAdminAsync(
                access,
                after.Id,
                AdminNotificationCategories.PlanNotes,
                AdminMutationAction.Updated,
                null,
                null,
                cancellationToken);
        }
    }

    private async Task TryNotifyAdminAsync(
        TravelPlanAccessResolution access,
        Guid travelPlanId,
        string category,
        AdminMutationAction action,
        string? itemLabel,
        Guid? relatedId,
        CancellationToken cancellationToken)
    {
        if (!access.IsAdminOverride)
            return;

        var adminId = ActingUserId();
        if (adminId is null)
            return;

        await _adminNotifications.NotifyPlanOwnerAsync(
            adminId.Value,
            travelPlanId,
            category,
            action,
            itemLabel,
            relatedId,
            null,
            cancellationToken);
    }

    private Guid? ActingUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var id = ActingUserId();
        if (id is null)
        {
            userId = default;
            return false;
        }

        userId = id.Value;
        return true;
    }
}
