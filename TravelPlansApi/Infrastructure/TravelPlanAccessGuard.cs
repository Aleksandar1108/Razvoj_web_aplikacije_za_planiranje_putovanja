using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrossService.Access;
using CrossService.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelPlansApi.Data;

namespace TravelPlansApi.Infrastructure;

public sealed class TravelPlanAccessGuard : ITravelPlanAccessGuard
{
    private readonly TravelPlansDbContext _db;
    private readonly ISharingInternalClient _sharing;

    public TravelPlanAccessGuard(TravelPlansDbContext db, ISharingInternalClient sharing)
    {
        _db = db;
        _sharing = sharing;
    }

    public async Task<TravelPlanAccessResolution> ResolveAsync(
        HttpContext httpContext,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        if (httpContext.Request.Headers.ContainsKey(ITravelPlanAccessGuard.ShareTokenHeaderName))
        {
            var kind = await _sharing.ResolveShareTokenAccessAsync(travelPlanId, requiresMutation, cancellationToken);
            return MapKind(kind);
        }

        if (!TryGetUserId(httpContext.User, out var userId))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

        if (httpContext.User.IsInRole("Admin")
            && await _db.TravelPlans.AsNoTracking().AnyAsync(p => p.Id == travelPlanId, cancellationToken))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);

        var owned = await _db.TravelPlans.AsNoTracking()
            .AnyAsync(p => p.Id == travelPlanId && p.UserId == userId, cancellationToken);

        if (owned)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Owner);

        var recipientKind = await _sharing.ResolveRecipientAccessAsync(travelPlanId, requiresMutation, cancellationToken);
        return MapKind(recipientKind);
    }

    private static TravelPlanAccessResolution MapKind(string kind) =>
        kind.Trim().ToLowerInvariant() switch
        {
            "shareview" => new TravelPlanAccessResolution(TravelPlanAccessKind.ShareView),
            "shareedit" => new TravelPlanAccessResolution(TravelPlanAccessKind.ShareEdit),
            _ => new TravelPlanAccessResolution(TravelPlanAccessKind.None)
        };

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
