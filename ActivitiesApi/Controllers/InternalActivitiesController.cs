using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ActivitiesApi.Data;
using CrossService.Dtos;

namespace ActivitiesApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}/activities")]
public sealed class InternalActivitiesController : ControllerBase
{
    private readonly ActivitiesDbContext _db;

    public InternalActivitiesController(ActivitiesDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("estimated-cost-sum")]
    public async Task<ActionResult<ActivityCostSumDto>> EstimatedCostSum(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var total = await _db.TravelActivities.AsNoTracking()
            .Where(a => a.TravelPlanId == travelPlanId)
            .SumAsync(a => (decimal?)a.EstimatedCost, cancellationToken) ?? 0m;

        return Ok(new ActivityCostSumDto { TotalEstimatedCost = total });
    }
}
