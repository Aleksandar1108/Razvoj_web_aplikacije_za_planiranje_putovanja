using CrossService.Access;
using CrossService.Clients;
using CrossService.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace CrossService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrossServiceRemoting(this IServiceCollection services)
    {
        services.AddSingleton<ITravelPlansInternalClient, TravelPlansInternalClient>();
        services.AddSingleton<ISharingInternalClient, SharingInternalClient>();
        services.AddSingleton<IWeb1InternalClient, Web1InternalClient>();
        services.AddSingleton<IActivitiesInternalClient, ActivitiesInternalClient>();
        services.AddSingleton<INotificationDispatcherInternalClient, NotificationDispatcherInternalClient>();

        services.AddScoped<ITravelPlanAccessGuard, RemotingTravelPlanAccessGuard>();
        services.AddScoped<IAdminNotificationPublisher, QueuedAdminNotificationPublisher>();

        return services;
    }

    public static IServiceCollection AddPlanCascadeDeleteClient(this IServiceCollection services)
    {
        services.AddSingleton<IPlanCascadeDeleteClient, PlanCascadeDeleteClient>();
        return services;
    }
}
