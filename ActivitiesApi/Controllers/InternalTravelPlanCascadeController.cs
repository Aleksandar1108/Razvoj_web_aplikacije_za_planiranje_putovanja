using ActivitiesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivitiesApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly IActivityService _activities;

    public InternalTravelPlanCascadeController(IActivityService activities)
    {
        _activities = activities;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var deleted = await _activities.DeleteAllByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(new { deleted });
    }
}
