using ExpensesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public InternalTravelPlanCascadeController(IExpenseService expenses)
    {
        _expenses = expenses;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var deleted = await _expenses.DeleteAllByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(new { deleted });
    }
}
