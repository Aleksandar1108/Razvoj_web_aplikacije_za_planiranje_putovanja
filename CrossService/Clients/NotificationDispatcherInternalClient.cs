using ServiceContracts.Dtos;
using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface INotificationDispatcherInternalClient
{
    Task EnqueueAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken);
}

public sealed class NotificationDispatcherInternalClient : INotificationDispatcherInternalClient
{
    private readonly INotificationDispatcherRemotingService _proxy;

    public NotificationDispatcherInternalClient()
    {
        _proxy = ServiceFabricRemoting.CreateProxy<INotificationDispatcherRemotingService>(
            ServiceFabricRemoting.ServiceNames.NotificationDispatcher);
    }

    public Task EnqueueAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken) =>
        _proxy.EnqueueAdminNotificationAsync(request, cancellationToken);
}
