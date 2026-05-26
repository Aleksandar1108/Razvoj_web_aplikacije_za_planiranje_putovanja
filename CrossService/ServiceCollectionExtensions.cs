using CrossService.Access;
using CrossService.Clients;
using CrossService.Http;
using CrossService.Notifications;
using CrossService.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrossService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrossServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MicroserviceUrlsOptions>(configuration.GetSection(MicroserviceUrlsOptions.SectionName));
        services.AddHttpContextAccessor();

        services.AddTransient<ForwardAuthHandler>();

        services.AddHttpClient<ITravelPlansInternalClient, TravelPlansInternalClient>()
            .AddHttpMessageHandler<ForwardAuthHandler>();
        services.AddHttpClient<ISharingInternalClient, SharingInternalClient>()
            .AddHttpMessageHandler<ForwardAuthHandler>();
        services.AddHttpClient<IWeb1InternalClient, Web1InternalClient>()
            .AddHttpMessageHandler<ForwardAuthHandler>();
        services.AddHttpClient<IActivitiesInternalClient, ActivitiesInternalClient>()
            .AddHttpMessageHandler<ForwardAuthHandler>();

        services.AddScoped<ITravelPlanAccessGuard, RemoteTravelPlanAccessGuard>();
        services.AddScoped<IAdminNotificationPublisher, Web1AdminNotificationPublisher>();

        return services;
    }
}
