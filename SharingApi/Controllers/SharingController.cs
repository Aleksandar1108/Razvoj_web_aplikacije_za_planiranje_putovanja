using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingApi.Dtos;
using SharingApi.Services;

namespace SharingApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class SharingController : ControllerBase
{
    private readonly ISharingService _sharing;

    public SharingController(ISharingService sharing)
    {
        _sharing = sharing;
    }

    [Authorize]
    [HttpPost("travel-plans/{travelPlanId:guid}/share-links")]
    [ProducesResponseType(typeof(CreateTravelPlanShareLinkResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateTravelPlanShareLinkResponseDto>> CreateShareLink(
        Guid travelPlanId,
        [FromBody] CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _sharing.CreateShareLinkAsync(userId, travelPlanId, request, cancellationToken);
            return Created(string.Empty, created);
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

    [Authorize]
    [HttpGet("shared-plans")]
    [ProducesResponseType(typeof(IReadOnlyList<SharedTravelPlanListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SharedTravelPlanListItemDto>>> ListSharedPlans(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var rows = await _sharing.ListSharedPlansForUserAsync(userId, cancellationToken);
        return Ok(rows);
    }

    [Authorize]
    [HttpPost("share-links/claim")]
    [ProducesResponseType(typeof(ClaimShareLinkResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimShareLinkResponseDto>> Claim(
        [FromBody] ClaimShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _sharing.ClaimShareLinkAsync(userId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
