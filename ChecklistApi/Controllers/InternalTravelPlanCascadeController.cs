using ChecklistApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly IChecklistService _checklist;

    public InternalTravelPlanCascadeController(IChecklistService checklist)
    {
        _checklist = checklist;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var deleted = await _checklist.DeleteAllByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(new { deleted });
    }
}
