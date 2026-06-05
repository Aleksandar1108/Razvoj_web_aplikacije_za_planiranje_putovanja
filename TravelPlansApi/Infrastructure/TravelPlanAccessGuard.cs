using CrossService.Access;
using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
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

        if (context.IsAdmin && await _db.TravelPlans.AsNoTracking().AnyAsync(p => p.Id == travelPlanId, cancellationToken))
            return new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);

        var owned = await _db.TravelPlans.AsNoTracking()
            .AnyAsync(p => p.Id == travelPlanId && p.UserId == userId, cancellationToken);

        if (owned)
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
