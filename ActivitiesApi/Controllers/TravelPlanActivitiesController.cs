using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ActivitiesApi.Dtos;
using ActivitiesApi.Services;
using CrossService.Access;
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
    private readonly IAdminPlanNotificationService _adminNotifications;

    public TravelPlanActivitiesController(
        IActivityService activities,
        ITravelPlanAccessGuard access,
        IAdminPlanNotificationService adminNotifications)
    {
        _activities = activities;
        _access = access;
        _adminNotifications = adminNotifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelActivityResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TravelActivityResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        return Ok(await _activities.ListByTravelPlanIdAsync(travelPlanId, cancellationToken));
    }

    [HttpGet("{activityId:guid}")]
    [ProducesResponseType(typeof(TravelActivityResponseDto), StatusCodes.Status200OK)]
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
            await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Created, created.Name, created.Id, null, cancellationToken);
            return CreatedAtAction(nameof(Get), new { travelPlanId, activityId = created.Id }, created);
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
            if (updated is null)
                return NotFound();
            await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Updated, updated.Name, updated.Id, null, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{activityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid travelPlanId, Guid activityId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        string? name = null;
        if (access.IsAdminOverride)
        {
            var existing = await _activities.GetAsync(travelPlanId, activityId, cancellationToken);
            name = existing?.Name;
        }

        var ok = await _activities.DeleteAsync(travelPlanId, activityId, cancellationToken);
        if (!ok)
            return NotFound();

        await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Deleted, name ?? "aktivnost", activityId, null, cancellationToken);
        return NoContent();
    }

    private async Task TryNotifyAsync(
        TravelPlanAccessResolution access,
        Guid travelPlanId,
        AdminMutationAction action,
        string itemLabel,
        Guid? relatedId,
        bool? checklistDone,
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
            AdminNotificationCategories.Activity,
            action,
            itemLabel,
            relatedId,
            checklistDone,
            cancellationToken);
    }

    private Guid? ActingUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
