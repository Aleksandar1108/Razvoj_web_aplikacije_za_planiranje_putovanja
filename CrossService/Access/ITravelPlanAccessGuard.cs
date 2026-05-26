using Microsoft.AspNetCore.Http;

namespace CrossService.Access;

public interface ITravelPlanAccessGuard
{
    const string ShareTokenHeaderName = "X-Share-Token";

    Task<TravelPlanAccessResolution> ResolveAsync(
        HttpContext httpContext,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken);
}
