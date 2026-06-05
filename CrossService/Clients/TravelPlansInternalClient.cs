using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface ITravelPlansInternalClient
{
    Task<TravelPlanMetaDto?> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelPlanOwnerDto> GetOwnerAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelPlanMetaDto>> GetMetaBatchAsync(ServiceCallContext context, IReadOnlyList<Guid> travelPlanIds, CancellationToken cancellationToken);
}

public sealed class TravelPlansInternalClient : ITravelPlansInternalClient
{
    private readonly ITravelPlansRemotingService _proxy;

    public TravelPlansInternalClient()
    {
        _proxy = ServiceFabricRemoting.CreateProxy<ITravelPlansRemotingService>(ServiceFabricRemoting.ServiceNames.TravelPlansApi);
    }

    public async Task<TravelPlanMetaDto?> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        try
        {
            return await _proxy.GetMetaAsync(travelPlanId, cancellationToken);
        }
        catch (ServiceOperationException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var dto = await _proxy.ExistsAsync(travelPlanId, cancellationToken);
        return dto.Exists;
    }

    public Task<TravelPlanOwnerDto> GetOwnerAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken) =>
        _proxy.GetOwnerAsync(context, travelPlanId, cancellationToken);

    public async Task<IReadOnlyList<TravelPlanMetaDto>> GetMetaBatchAsync(
        ServiceCallContext context,
        IReadOnlyList<Guid> travelPlanIds,
        CancellationToken cancellationToken)
    {
        if (travelPlanIds.Count == 0)
            return Array.Empty<TravelPlanMetaDto>();

        return await _proxy.GetMetaBatchAsync(
            context,
            new SharedPlanMetaBatchRequestDto { TravelPlanIds = travelPlanIds.ToList() },
            cancellationToken);
    }
}
