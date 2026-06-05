using ServiceContracts;

namespace CrossService.Access;

public interface ITravelPlanAccessGuard
{
    const string ShareTokenHeaderName = "X-Share-Token";

    Task<TravelPlanAccessResolution> ResolveAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken);
}
