using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChecklistApi.Dtos;
using ChecklistApi.Services;
using CrossService.Access;
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
    private readonly IAdminPlanNotificationService _adminNotifications;

    public TravelPlanChecklistController(
        IChecklistService checklist,
        ITravelPlanAccessGuard access,
        IAdminPlanNotificationService adminNotifications)
    {
        _checklist = checklist;
        _access = access;
        _adminNotifications = adminNotifications;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        return Ok(await _checklist.ListByTravelPlanIdAsync(travelPlanId, cancellationToken));
    }

    [HttpPost]
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
            await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Created, created.Title, created.Id, null, cancellationToken);
            return CreatedAtAction(nameof(Get), new { travelPlanId, itemId = created.Id }, created);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<ChecklistItemResponseDto>> Get(
        Guid travelPlanId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _checklist.GetAsync(travelPlanId, itemId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{itemId:guid}")]
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
        if (updated is null)
            return NotFound();
        await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Updated, updated.Title, updated.Id, null, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{itemId:guid}/toggle")]
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
        if (updated is null)
            return NotFound();
        await TryNotifyAsync(
            access,
            travelPlanId,
            AdminMutationAction.Toggled,
            updated.Title,
            updated.Id,
            updated.IsDone,
            cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        string? title = null;
        if (access.IsAdminOverride)
        {
            var existing = await _checklist.GetAsync(travelPlanId, itemId, cancellationToken);
            title = existing?.Title;
        }

        var ok = await _checklist.DeleteAsync(travelPlanId, itemId, cancellationToken);
        if (!ok)
            return NotFound();

        await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Deleted, title ?? "stavku", itemId, null, cancellationToken);
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
            AdminNotificationCategories.Checklist,
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
