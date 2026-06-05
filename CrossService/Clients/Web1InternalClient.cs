using ServiceContracts.Dtos;
using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface IWeb1InternalClient
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, UserBriefDto>> GetUsersBriefAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);
    Task CreateAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken);
}

public sealed class Web1InternalClient : IWeb1InternalClient
{
    private readonly IWeb1RemotingService _proxy;

    public Web1InternalClient()
    {
        _proxy = ServiceFabricRemoting.CreateProxy<IWeb1RemotingService>(ServiceFabricRemoting.ServiceNames.Web1);
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var dto = await _proxy.UserExistsAsync(userId, cancellationToken);
        return dto.Exists;
    }

    public async Task<IReadOnlyDictionary<Guid, UserBriefDto>> GetUsersBriefAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var list = await _proxy.GetUsersBriefAsync(
            new UsersBriefRequestDto { UserIds = userIds.ToList() },
            cancellationToken);
        return list.ToDictionary(u => u.Id);
    }

    public Task CreateAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken) =>
        _proxy.CreateAdminNotificationAsync(request, cancellationToken);
}
