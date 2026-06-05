using CrossService.Clients;
using ServiceContracts.Dtos;

namespace CrossService.Notifications;

public interface IAdminNotificationPublisher
{
    Task NotifyPlanOwnerAsync(
        Guid ownerUserId,
        Guid travelPlanId,
        string category,
        string action,
        string? itemLabel = null,
        Guid? relatedEntityId = null,
        bool? checklistDone = null,
        CancellationToken cancellationToken = default);
}

public sealed class QueuedAdminNotificationPublisher : IAdminNotificationPublisher
{
    private readonly INotificationDispatcherInternalClient _dispatcher;

    public QueuedAdminNotificationPublisher(INotificationDispatcherInternalClient dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task NotifyPlanOwnerAsync(
        Guid ownerUserId,
        Guid travelPlanId,
        string category,
        string action,
        string? itemLabel = null,
        Guid? relatedEntityId = null,
        bool? checklistDone = null,
        CancellationToken cancellationToken = default) =>
        _dispatcher.EnqueueAdminNotificationAsync(
            new CreateAdminNotificationRequestDto
            {
                OwnerUserId = ownerUserId,
                TravelPlanId = travelPlanId,
                Category = category,
                Action = action,
                ItemLabel = itemLabel,
                RelatedEntityId = relatedEntityId,
                ChecklistDone = checklistDone
            },
            cancellationToken);
}
