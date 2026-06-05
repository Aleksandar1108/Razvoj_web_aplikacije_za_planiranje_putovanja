using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface IActivitiesInternalClient
{
    Task<decimal> GetEstimatedCostSumAsync(Guid travelPlanId, CancellationToken cancellationToken);
}

public sealed class ActivitiesInternalClient : IActivitiesInternalClient
{
    private readonly IActivitiesRemotingService _proxy;

    public ActivitiesInternalClient()
    {
        _proxy = ServiceFabricRemoting.CreateProxy<IActivitiesRemotingService>(ServiceFabricRemoting.ServiceNames.ActivitiesApi);
    }

    public async Task<decimal> GetEstimatedCostSumAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _proxy.GetEstimatedCostSumAsync(travelPlanId, cancellationToken);
            return dto.TotalEstimatedCost;
        }
        catch (ServiceContracts.ServiceOperationException ex) when (ex.StatusCode == 404)
        {
            return 0m;
        }
    }
}
