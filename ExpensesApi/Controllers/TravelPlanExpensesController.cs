using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExpensesApi.Dtos;
using ExpensesApi.Services;
using CrossService.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/expenses")]
public sealed class TravelPlanExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;
    private readonly ITravelPlanAccessGuard _access;
    private readonly IAdminPlanNotificationService _adminNotifications;

    public TravelPlanExpensesController(
        IExpenseService expenses,
        ITravelPlanAccessGuard access,
        IAdminPlanNotificationService adminNotifications)
    {
        _expenses = expenses;
        _access = access;
        _adminNotifications = adminNotifications;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TravelExpenseResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        return Ok(await _expenses.ListByTravelPlanIdAsync(travelPlanId, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ExpenseSummaryDto>> Summary(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _expenses.GetSummaryAsync(travelPlanId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{expenseId:guid}")]
    public async Task<ActionResult<TravelExpenseResponseDto>> Get(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _expenses.GetAsync(travelPlanId, expenseId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<TravelExpenseResponseDto>> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelExpenseRequestDto request,
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
            var created = await _expenses.CreateAsync(travelPlanId, request, cancellationToken);
            await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Created, created.Name, created.Id, null, cancellationToken);
            return CreatedAtAction(nameof(Get), new { travelPlanId, expenseId = created.Id }, created);
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

    [HttpPut("{expenseId:guid}")]
    public async Task<ActionResult<TravelExpenseResponseDto>> Update(
        Guid travelPlanId,
        Guid expenseId,
        [FromBody] UpdateTravelExpenseRequestDto request,
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
            var updated = await _expenses.UpdateAsync(travelPlanId, expenseId, request, cancellationToken);
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

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();

        string? name = null;
        if (access.IsAdminOverride)
        {
            var existing = await _expenses.GetAsync(travelPlanId, expenseId, cancellationToken);
            name = existing?.Name;
        }

        var ok = await _expenses.DeleteAsync(travelPlanId, expenseId, cancellationToken);
        if (!ok)
            return NotFound();

        await TryNotifyAsync(access, travelPlanId, AdminMutationAction.Deleted, name ?? "trosak", expenseId, null, cancellationToken);
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
            AdminNotificationCategories.Expense,
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
