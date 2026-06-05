using System.Fabric;
using CrossService;
using CrossService.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;
using DestinationsApi.Data;
using DestinationsApi.Services;

namespace DestinationsApi;

internal sealed class DestinationsApiService : StatelessService, IDestinationsRemotingService
{
    private readonly IServiceProvider _services;

    public DestinationsApiService(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<DestinationsDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddScoped<IDestinationService, DestinationService>();
            services.AddScoped<IAdminDestinationService, AdminDestinationService>();
            services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<List<TravelDestinationResponseDto>> ListDestinationsAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            return (await sp.GetRequiredService<IDestinationService>().ListByTravelPlanIdAsync(travelPlanId, ct)).ToList();
        }, cancellationToken);

    public Task<TravelDestinationResponseDto> GetDestinationAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<IDestinationService>().GetAsync(travelPlanId, destinationId, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Destinacija nije pronađena.");
            return dto;
        }, cancellationToken);

    public Task<TravelDestinationResponseDto> CreateDestinationAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var created = await sp.GetRequiredService<IDestinationService>().CreateAsync(travelPlanId, request, ct);
                await TryNotifyAdminActionAsync(
                    sp, access, context, travelPlanId, AdminMutationAction.Created, created.Name, created.Id, ct);
                return created;
            }
            catch (InvalidOperationException)
            {
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task<TravelDestinationResponseDto> UpdateDestinationAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid destinationId,
        UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var updated = await sp.GetRequiredService<IDestinationService>()
                    .UpdateAsync(travelPlanId, destinationId, request, ct);
                if (updated is null)
                    throw new ServiceOperationException(404, "Destinacija nije pronađena.");

                await TryNotifyAdminActionAsync(
                    sp, access, context, travelPlanId, AdminMutationAction.Updated, updated.Name, updated.Id, ct);
                return updated;
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task DeleteDestinationAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var destinations = sp.GetRequiredService<IDestinationService>();
            string? nameForNotify = null;
            if (access.IsAdminOverride)
            {
                var existing = await destinations.GetAsync(travelPlanId, destinationId, ct);
                nameForNotify = existing?.Name;
            }

            var ok = await destinations.DeleteAsync(travelPlanId, destinationId, ct);
            if (!ok)
                throw new ServiceOperationException(404, "Destinacija nije pronađena.");

            if (access.IsAdminOverride)
            {
                await TryNotifyAdminActionAsync(
                    sp,
                    access,
                    context,
                    travelPlanId,
                    AdminMutationAction.Deleted,
                    nameForNotify ?? "destinacija",
                    destinationId,
                    ct);
            }
        }, cancellationToken);

    public Task<List<AdminDestinationListItemDto>> ListAdminDestinationsAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            return (await sp.GetRequiredService<IAdminDestinationService>().ListAllAsync(context, ct)).ToList();
        }, cancellationToken);

    public Task<List<AdminTravelPlanOptionDto>> ListAdminTravelPlanOptionsAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            return (await sp.GetRequiredService<IAdminDestinationService>().ListTravelPlansAsync(context, ct)).ToList();
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var deleted = await sp.GetRequiredService<IDestinationService>()
                .DeleteAllByTravelPlanIdAsync(travelPlanId, ct);
            return new CascadeDeleteResultDto { Deleted = deleted };
        }, cancellationToken);

    private static async Task<TravelPlanAccessResolution> RequireAllowedAccess(
        IServiceProvider sp,
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        var access = await sp.GetRequiredService<ITravelPlanAccessGuard>()
            .ResolveAsync(context, travelPlanId, requiresMutation, cancellationToken);
        if (!access.IsAllowed)
            throw new ServiceOperationException(401, "Nemate pristup ovom planu.");
        return access;
    }

    private static void RequireMutate(TravelPlanAccessResolution access)
    {
        if (!access.CanMutate)
            throw new ServiceOperationException(403, "Nemate dozvolu za izmenu.");
    }

    private static async Task TryNotifyAdminActionAsync(
        IServiceProvider sp,
        TravelPlanAccessResolution access,
        ServiceCallContext context,
        Guid travelPlanId,
        AdminMutationAction action,
        string destinationName,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        if (!access.IsAdminOverride || context.UserId is not { } adminId)
            return;

        await sp.GetRequiredService<IAdminPlanNotificationService>().NotifyPlanOwnerAsync(
            adminId,
            travelPlanId,
            AdminNotificationCategories.Destination,
            action,
            destinationName,
            destinationId,
            null,
            cancellationToken);
    }
}
