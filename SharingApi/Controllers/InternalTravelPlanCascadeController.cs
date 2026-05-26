using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingApi.Services;

namespace SharingApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly ISharingService _sharing;

    public InternalTravelPlanCascadeController(ISharingService sharing)
    {
        _sharing = sharing;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var deleted = await _sharing.DeleteAllByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(new { deleted });
    }
}
