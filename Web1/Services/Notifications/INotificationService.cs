using Web1.Dtos.Notifications;

namespace Web1.Services.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<UserNotificationDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
