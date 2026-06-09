using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface IWeb1RemotingService : IService
{
    Task<AuthOperationResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    Task<AuthOperationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthUserDto> GetMeAsync(ServiceCallContext context, CancellationToken cancellationToken);

    Task<AdminSystemStatsDto> GetAdminStatsAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<List<AdminUserListItemDto>> ListAdminUsersAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<AdminUserListItemDto> GetAdminUserAsync(ServiceCallContext context, Guid userId, CancellationToken cancellationToken);
    Task<AdminUserListItemDto> CreateAdminUserAsync(ServiceCallContext context, CreateAdminUserRequestDto request, CancellationToken cancellationToken);
    Task<AdminUserListItemDto> UpdateAdminUserAsync(ServiceCallContext context, Guid userId, UpdateAdminUserRequestDto request, CancellationToken cancellationToken);
    Task DeleteAdminUserAsync(ServiceCallContext context, Guid userId, CancellationToken cancellationToken);

    Task<List<UserNotificationDto>> ListNotificationsAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<UnreadNotificationCountDto> GetUnreadNotificationCountAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task MarkNotificationReadAsync(ServiceCallContext context, Guid notificationId, CancellationToken cancellationToken);
    Task MarkAllNotificationsReadAsync(ServiceCallContext context, CancellationToken cancellationToken);

    Task<UserExistsDto> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<UserBriefDto>> GetUsersBriefAsync(UsersBriefRequestDto request, CancellationToken cancellationToken);
    Task CreateAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken);
    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
