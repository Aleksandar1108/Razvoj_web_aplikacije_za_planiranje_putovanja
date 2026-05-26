using CrossService.Clients;
using CrossService.Notifications;

namespace ExpensesApi.Services;

public interface IAdminPlanNotificationService
{
    Task NotifyPlanOwnerAsync(
        Guid actingAdminUserId,
        Guid travelPlanId,
        string category,
        AdminMutationAction action,
        string? itemLabel = null,
        Guid? relatedEntityId = null,
        bool? checklistDone = null,
        CancellationToken cancellationToken = default);
}

public enum AdminMutationAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Toggled = 4
}

public static class AdminNotificationCategories
{
    public const string PlanBasic = "admin_plan_basic";
    public const string PlanNotes = "admin_plan_notes";
    public const string Destination = "admin_destination";
    public const string Expense = "admin_expense";
    public const string Activity = "admin_activity";
    public const string Checklist = "admin_checklist";
}

public sealed class AdminPlanNotificationService : IAdminPlanNotificationService
{
    private readonly IAdminNotificationPublisher _publisher;
    private readonly ITravelPlansInternalClient _travelPlans;

    public AdminPlanNotificationService(
        IAdminNotificationPublisher publisher,
        ITravelPlansInternalClient travelPlans)
    {
        _publisher = publisher;
        _travelPlans = travelPlans;
    }

    public async Task NotifyPlanOwnerAsync(
        Guid actingAdminUserId,
        Guid travelPlanId,
        string category,
        AdminMutationAction action,
        string? itemLabel = null,
        Guid? relatedEntityId = null,
        bool? checklistDone = null,
        CancellationToken cancellationToken = default)
    {
        var meta = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken);
        if (meta is null || meta.UserId == actingAdminUserId)
            return;

        await _publisher.NotifyPlanOwnerAsync(
            meta.UserId,
            travelPlanId,
            category,
            action.ToString(),
            itemLabel,
            relatedEntityId,
            checklistDone,
            cancellationToken);
    }
}
