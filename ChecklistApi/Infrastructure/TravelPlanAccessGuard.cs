using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChecklistApi.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChecklistApi.Infrastructure;

public enum TravelPlanAccessKind
{
    None = 0,
    Owner = 1,
    ShareView = 2,
    ShareEdit = 3
}

public readonly record struct TravelPlanAccessResolution(TravelPlanAccessKind Kind)
{
    public bool IsAllowed => Kind != TravelPlanAccessKind.None;
    public bool CanMutate => Kind is TravelPlanAccessKind.Owner or TravelPlanAccessKind.ShareEdit;
}

public interface ITravelPlanAccessGuard
{
    Task<TravelPlanAccessResolution> ResolveAsync(
        HttpContext httpContext,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken);
}

public sealed class TravelPlanAccessGuard : ITravelPlanAccessGuard
{
    public const string ShareTokenHeaderName = "X-Share-Token";

    private readonly ChecklistDbContext _db;

    public TravelPlanAccessGuard(ChecklistDbContext db)
    {
        _db = db;
    }

    public async Task<TravelPlanAccessResolution> ResolveAsync(
        HttpContext httpContext,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        if (httpContext.Request.Headers.TryGetValue(ShareTokenHeaderName, out var rawValues))
        {
            var token = rawValues.FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(token))
                return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

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
                return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

            var perm = link.Permission.Trim().ToLowerInvariant();
            var kind = perm == "edit"
                ? TravelPlanAccessKind.ShareEdit
                : TravelPlanAccessKind.ShareView;

            if (requiresMutation && kind != TravelPlanAccessKind.ShareEdit)
                return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

            return new TravelPlanAccessResolution(kind);
        }

        if (!TryGetUserId(httpContext.User, out var userId))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

        var owned = await _db.TravelPlans.AsNoTracking()
            .AnyAsync(p => p.Id == travelPlanId && p.UserId == userId, cancellationToken);

        if (owned)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Owner);

        var recipient = await _db.TravelPlanShareRecipients.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TravelPlanId == travelPlanId && r.RecipientUserId == userId, cancellationToken);

        if (recipient is null)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

        var recipientPerm = recipient.Permission.Trim().ToLowerInvariant();
        var recipientKind = recipientPerm == "edit"
            ? TravelPlanAccessKind.ShareEdit
            : TravelPlanAccessKind.ShareView;

        if (requiresMutation && recipientKind != TravelPlanAccessKind.ShareEdit)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

        return new TravelPlanAccessResolution(recipientKind);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
