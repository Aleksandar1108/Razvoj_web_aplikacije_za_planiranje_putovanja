using ServiceContracts;
using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface ISharingInternalClient
{
    Task<string> ResolveShareTokenAccessAsync(ServiceCallContext context, Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);
    Task<string> ResolveRecipientAccessAsync(ServiceCallContext context, Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);
}

public sealed class SharingInternalClient : ISharingInternalClient
{
    private readonly ISharingRemotingService _proxy;

    public SharingInternalClient()
    {
        _proxy = ServiceFabricRemoting.CreateProxy<ISharingRemotingService>(ServiceFabricRemoting.ServiceNames.SharingApi);
    }

    public async Task<string> ResolveShareTokenAccessAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        var dto = await _proxy.ResolveShareTokenAccessAsync(context, travelPlanId, requiresMutation, cancellationToken);
        return dto.Kind;
    }

    public async Task<string> ResolveRecipientAccessAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _proxy.ResolveRecipientAccessAsync(context, travelPlanId, requiresMutation, cancellationToken);
            return dto.Kind;
        }
        catch (ServiceOperationException ex) when (ex.StatusCode is 401 or 404)
        {
            return "none";
        }
    }
}
