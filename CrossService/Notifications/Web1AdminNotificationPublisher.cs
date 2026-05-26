using CrossService.Clients;
using CrossService.Dtos;

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

public sealed class Web1AdminNotificationPublisher : IAdminNotificationPublisher
{
    private readonly IWeb1InternalClient _web1;

    public Web1AdminNotificationPublisher(IWeb1InternalClient web1)
    {
        _web1 = web1;
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
        _web1.CreateAdminNotificationAsync(
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
