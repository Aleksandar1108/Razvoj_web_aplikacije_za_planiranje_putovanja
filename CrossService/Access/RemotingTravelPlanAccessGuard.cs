using CrossService.Clients;
using ServiceContracts;

namespace CrossService.Access;

public sealed class RemotingTravelPlanAccessGuard : ITravelPlanAccessGuard
{
    private readonly ITravelPlansInternalClient _travelPlans;
    private readonly ISharingInternalClient _sharing;

    public RemotingTravelPlanAccessGuard(
        ITravelPlansInternalClient travelPlans,
        ISharingInternalClient sharing)
    {
        _travelPlans = travelPlans;
        _sharing = sharing;
    }

    public async Task<TravelPlanAccessResolution> ResolveAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(context.ShareToken))
        {
            var kind = await _sharing.ResolveShareTokenAccessAsync(context, travelPlanId, requiresMutation, cancellationToken);
            return MapKind(kind);
        }

        if (context.UserId is not { } userId)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.None);

        if (context.IsAdmin && await _travelPlans.ExistsAsync(travelPlanId, cancellationToken))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);

        var owner = await _travelPlans.GetOwnerAsync(context, travelPlanId, cancellationToken);
        if (owner.IsOwner && owner.OwnerUserId == userId)
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Owner);

        var recipientKind = await _sharing.ResolveRecipientAccessAsync(context, travelPlanId, requiresMutation, cancellationToken);
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
}
