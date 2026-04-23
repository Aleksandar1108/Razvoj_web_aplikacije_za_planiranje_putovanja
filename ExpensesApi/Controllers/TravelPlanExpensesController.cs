using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExpensesApi.Dtos;
using ExpensesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/travel-plans/{travelPlanId:guid}/expenses")]
public sealed class TravelPlanExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public TravelPlanExpensesController(IExpenseService expenses)
    {
        _expenses = expenses;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelExpenseResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TravelExpenseResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        if (await _expenses.GetOwnedPlanAsync(userId, travelPlanId, cancellationToken) is null)
            return NotFound();
        var list = await _expenses.ListByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExpenseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseSummaryDto>> Summary(Guid travelPlanId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var dto = await _expenses.GetSummaryAsync(userId, travelPlanId, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var dto = await _expenses.GetAsync(userId, travelPlanId, expenseId, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _expenses.CreateAsync(userId, travelPlanId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _expenses.UpdateAsync(userId, travelPlanId, expenseId, request, cancellationToken);
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var ok = await _expenses.DeleteAsync(userId, travelPlanId, expenseId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
