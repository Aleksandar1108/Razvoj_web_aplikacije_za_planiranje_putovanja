using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web1.Dtos.Admin;
using Web1.Services.Admin;

namespace Web1.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    private Guid? ActingUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminSystemStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminSystemStatsDto>> Stats(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetStatsAsync(cancellationToken));
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserListItemDto>>> ListUsers(CancellationToken cancellationToken)
    {
        return Ok(await _admin.ListUsersAsync(cancellationToken));
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserListItemDto>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var u = await _admin.GetUserAsync(userId, cancellationToken);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPatch("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserListItemDto>> UpdateUser(
        Guid userId,
        [FromBody] UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var adminId = ActingUserId();
        if (adminId is null)
            return Unauthorized();

        var (ok, error, updated) = await _admin.UpdateUserAsync(adminId.Value, userId, request, cancellationToken);
        if (!ok && error == "Korisnik nije pronađen.")
            return NotFound(new { message = error });
        if (!ok)
            return BadRequest(new { message = error });

        return Ok(updated);
    }
}
