using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrossService.Clients;
using Microsoft.AspNetCore.Http;

namespace CrossService.Access;

public sealed class RemoteTravelPlanAccessGuard : ITravelPlanAccessGuard
{
    private readonly ITravelPlansInternalClient _travelPlans;
    private readonly ISharingInternalClient _sharing;

    public RemoteTravelPlanAccessGuard(
        ITravelPlansInternalClient travelPlans,
        ISharingInternalClient sharing)
    {
        _travelPlans = travelPlans;
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
            && await _travelPlans.ExistsAsync(travelPlanId, cancellationToken))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);

        var owner = await _travelPlans.GetOwnerAsync(travelPlanId, cancellationToken);
        if (owner.IsOwner && owner.OwnerUserId == userId)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Owner);

        var recipientKind = await _sharing.ResolveRecipientAccessAsync(travelPlanId, requiresMutation, cancellationToken);
        return MapKind(recipientKind);
    }

    private static TravelPlanAccessResolution MapKind(string kind) =>
        kind.Trim().ToLowerInvariant() switch
        {
            "owner" => new TravelPlanAccessResolution(TravelPlanAccessKind.Owner),
            "shareview" => new TravelPlanAccessResolution(TravelPlanAccessKind.ShareView),
            "shareedit" => new TravelPlanAccessResolution(TravelPlanAccessKind.ShareEdit),
            "admin" => new TravelPlanAccessResolution(TravelPlanAccessKind.Admin),
            _ => new TravelPlanAccessResolution(TravelPlanAccessKind.None)
        };

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }
}
