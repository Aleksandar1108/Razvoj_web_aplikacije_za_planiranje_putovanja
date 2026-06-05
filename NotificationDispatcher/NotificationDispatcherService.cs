using System.Fabric;
using CrossService;
using CrossService.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NotificationDispatcher.Infrastructure;
using NotificationDispatcher.Models;
using NotificationDispatcher.Services;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;

namespace NotificationDispatcher;

internal sealed class NotificationDispatcherService : StatefulService, INotificationDispatcherRemotingService
{
    private readonly IServiceProvider _services;

    public NotificationDispatcherService(StatefulServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            services.AddSingleton(context);
            services.AddSingleton(StateManager);
            services.AddSingleton<IReliableStateManager>(StateManager);
            services.AddSingleton<INotificationQueueStore, NotificationQueueStore>();
            services.AddSingleton<IHostEnvironment, ServiceFabricHostEnvironment>();
            services.AddLogging();
            services.AddCrossServiceRemoting();
        });
    }

    protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() =>
        this.CreateServiceRemotingReplicaListeners();

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var queue = _services.GetRequiredService<INotificationQueueStore>();
        var web1 = _services.GetRequiredService<IWeb1InternalClient>();
        var logger = _services.GetRequiredService<ILogger<NotificationDispatcherService>>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await queue.TryDequeueAsync(cancellationToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    continue;
                }

                await web1.CreateAdminNotificationAsync(
                    new CreateAdminNotificationRequestDto
                    {
                        OwnerUserId = message.OwnerUserId,
                        TravelPlanId = message.TravelPlanId,
                        Category = message.Category,
                        Action = message.Action,
                        ItemLabel = message.ItemLabel,
                        RelatedEntityId = message.RelatedEntityId,
                        ChecklistDone = message.ChecklistDone
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Greška pri obradi admin obaveštenja iz Reliable Queue.");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task EnqueueAdminNotificationAsync(
        CreateAdminNotificationRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await sp.GetRequiredService<INotificationQueueStore>().EnqueueAsync(
                new NotificationQueueMessage
                {
                    OwnerUserId = request.OwnerUserId,
                    TravelPlanId = request.TravelPlanId,
                    Category = request.Category,
                    Action = request.Action,
                    ItemLabel = request.ItemLabel,
                    RelatedEntityId = request.RelatedEntityId,
                    ChecklistDone = request.ChecklistDone
                },
                ct);
        }, cancellationToken);
}
