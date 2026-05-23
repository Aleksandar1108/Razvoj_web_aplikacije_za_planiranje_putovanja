using ExpensesApi.Dtos;
using ExpensesApi.Infrastructure;
using ExpensesApi.Services;
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

    public TravelPlanExpensesController(IExpenseService expenses, ITravelPlanAccessGuard access)
    {
        _expenses = expenses;
        _access = access;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelExpenseResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelExpenseResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var list = await _expenses.ListByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExpenseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseSummaryDto>> Summary(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        var dto = await _expenses.GetSummaryAsync(travelPlanId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{expenseId:guid}")]
    [ProducesResponseType(typeof(TravelExpenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(TravelExpenseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(TravelExpenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{expenseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, travelPlanId, requiresMutation: true, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();
        if (!access.CanMutate)
            return Forbid();
        var ok = await _expenses.DeleteAsync(travelPlanId, expenseId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
