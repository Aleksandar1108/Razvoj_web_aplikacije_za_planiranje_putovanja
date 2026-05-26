using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingApi.Data;
using SharingApi.Infrastructure;
using CrossService.Dtos;

namespace SharingApi.Controllers;

[ApiController]
[Route("api/v1/internal/access")]
public sealed class InternalAccessController : ControllerBase
{
    public const string ShareTokenHeaderName = "X-Share-Token";

    private readonly SharingDbContext _db;

    public InternalAccessController(SharingDbContext db)
    {
        _db = db;
    }

    [HttpGet("share-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ShareAccessDto>> ShareToken(
        [FromQuery] Guid travelPlanId,
        [FromQuery] bool requiresMutation,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(ShareTokenHeaderName, out var rawValues))
            return Ok(new ShareAccessDto { Kind = "none" });

        var token = rawValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return Ok(new ShareAccessDto { Kind = "none" });

        var tokenHash = ShareTokenCrypto.HashToken(token);
        var now = DateTime.UtcNow;

        var link = await _db.TravelPlanShareLinks.AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.TokenHash == tokenHash
                     && l.TravelPlanId == travelPlanId
                     && l.RevokedAtUtc == null
                     && (l.ExpiresAtUtc == null || l.ExpiresAtUtc > now),
                cancellationToken);

        if (link is null)
            return Ok(new ShareAccessDto { Kind = "none" });

        var perm = link.Permission.Trim().ToLowerInvariant();
        if (requiresMutation && perm != "edit")
            return Ok(new ShareAccessDto { Kind = "none" });

        return Ok(new ShareAccessDto { Kind = perm == "edit" ? "shareEdit" : "shareView" });
    }

    [HttpGet("recipient")]
    [Authorize]
    public async Task<ActionResult<ShareAccessDto>> Recipient(
        [FromQuery] Guid travelPlanId,
        [FromQuery] bool requiresMutation,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var recipient = await _db.TravelPlanShareRecipients.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TravelPlanId == travelPlanId && r.RecipientUserId == userId, cancellationToken);

        if (recipient is null)
            return Ok(new ShareAccessDto { Kind = "none" });

        var perm = recipient.Permission.Trim().ToLowerInvariant();
        if (requiresMutation && perm != "edit")
            return Ok(new ShareAccessDto { Kind = "none" });

        return Ok(new ShareAccessDto { Kind = perm == "edit" ? "shareEdit" : "shareView" });
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
