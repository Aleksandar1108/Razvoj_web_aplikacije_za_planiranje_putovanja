using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlansApi.Dtos;
using TravelPlansApi.Infrastructure;
using TravelPlansApi.Services;

namespace TravelPlansApi.Controllers;

[ApiController]
[Route("api/v1/travel-plans")]
public sealed class TravelPlansController : ControllerBase
{
    private readonly ITravelPlanService _plans;
    private readonly ITravelPlanAccessGuard _access;

    public TravelPlansController(ITravelPlanService plans, ITravelPlanAccessGuard access)
    {
        _plans = plans;
        _access = access;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TravelPlanResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TravelPlanResponseDto>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var list = await _plans.ListForUserAsync(userId, cancellationToken);
        return Ok(list);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPlanResponseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var access = await _access.ResolveAsync(HttpContext, id, requiresMutation: false, cancellationToken);
        if (!access.IsAllowed)
            return Unauthorized();

        var dto = await _plans.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TravelPlanResponseDto>> Create(
        [FromBody] CreateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _plans.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPlanResponseDto>> Update(
        Guid id,
        [FromBody] UpdateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _plans.UpdateAsync(userId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var ok = await _plans.DeleteAsync(userId, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
