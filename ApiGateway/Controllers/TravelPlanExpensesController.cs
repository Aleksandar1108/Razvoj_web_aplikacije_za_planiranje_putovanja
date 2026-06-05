using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/travel-plans/{travelPlanId:guid}/expenses")]
public sealed class TravelPlanExpensesController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public TravelPlanExpensesController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<TravelExpenseResponseDto>>> List(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.ListExpensesAsync(context, travelPlanId, cancellationToken));
    }

    [HttpGet("summary")]
    public Task<ActionResult<ExpenseSummaryDto>> Summary(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.GetExpenseSummaryAsync(context, travelPlanId, cancellationToken));
    }

    [HttpGet("{expenseId:guid}")]
    public Task<ActionResult<TravelExpenseResponseDto>> Get(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.GetExpenseAsync(context, travelPlanId, expenseId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid travelPlanId,
        [FromBody] CreateTravelExpenseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.CreateExpenseAsync(context, travelPlanId, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return CreatedAtAction(
            nameof(Get),
            new { travelPlanId, expenseId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{expenseId:guid}")]
    public Task<ActionResult<TravelExpenseResponseDto>> Update(
        Guid travelPlanId,
        Guid expenseId,
        [FromBody] UpdateTravelExpenseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Task.FromResult<ActionResult<TravelExpenseResponseDto>>(ValidationProblem(ModelState));

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.UpdateExpenseAsync(context, travelPlanId, expenseId, request, cancellationToken));
    }

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Expenses.DeleteExpenseAsync(context, travelPlanId, expenseId, cancellationToken));
    }
}
