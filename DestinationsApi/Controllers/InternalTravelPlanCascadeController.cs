using DestinationsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DestinationsApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly IDestinationService _destinations;

    public InternalTravelPlanCascadeController(IDestinationService destinations)
    {
        _destinations = destinations;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var deleted = await _destinations.DeleteAllByTravelPlanIdAsync(travelPlanId, cancellationToken);
        return Ok(new { deleted });
    }
}
