using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface INotificationDispatcherRemotingService : IService
{
    Task EnqueueAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken);
}
