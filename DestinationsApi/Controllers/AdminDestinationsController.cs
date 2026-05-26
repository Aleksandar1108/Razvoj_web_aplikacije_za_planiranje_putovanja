using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DestinationsApi.Dtos;
using DestinationsApi.Services;

namespace DestinationsApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminDestinationsController : ControllerBase
{
    private readonly IAdminDestinationService _admin;

    public AdminDestinationsController(IAdminDestinationService admin)
    {
        _admin = admin;
    }

    [HttpGet("destinations")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminDestinationListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminDestinationListItemDto>>> ListDestinations(
        CancellationToken cancellationToken)
    {
        return Ok(await _admin.ListAllAsync(cancellationToken));
    }

    [HttpGet("travel-plans")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTravelPlanOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminTravelPlanOptionDto>>> ListTravelPlans(
        CancellationToken cancellationToken)
    {
        return Ok(await _admin.ListTravelPlansAsync(cancellationToken));
    }
}
