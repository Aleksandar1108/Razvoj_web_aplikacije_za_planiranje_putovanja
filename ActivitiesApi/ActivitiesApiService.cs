using System.Fabric;
using ActivitiesApi.Data;
using ActivitiesApi.Services;
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

namespace ActivitiesApi;

internal sealed class ActivitiesApiService : StatelessService, IActivitiesRemotingService
{
    private readonly IServiceProvider _services;

    public ActivitiesApiService(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<ActivitiesDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<List<TravelActivityResponseDto>> ListActivitiesAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            return (await sp.GetRequiredService<IActivityService>().ListByTravelPlanIdAsync(travelPlanId, ct)).ToList();
        }, cancellationToken);

    public Task<TravelActivityResponseDto> GetActivityAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<IActivityService>().GetAsync(travelPlanId, activityId, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Aktivnost nije pronađena.");
            return dto;
        }, cancellationToken);

    public Task<TravelActivityResponseDto> CreateActivityAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CreateTravelActivityRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var created = await sp.GetRequiredService<IActivityService>().CreateAsync(travelPlanId, request, ct);
                await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Created, created.Name, created.Id, null, ct);
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

    public Task<TravelActivityResponseDto> UpdateActivityAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid activityId,
        UpdateTravelActivityRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var updated = await sp.GetRequiredService<IActivityService>()
                    .UpdateAsync(travelPlanId, activityId, request, ct);
                if (updated is null)
                    throw new ServiceOperationException(404, "Aktivnost nije pronađena.");

                await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Updated, updated.Name, updated.Id, null, ct);
                return updated;
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task DeleteActivityAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var activities = sp.GetRequiredService<IActivityService>();
            string? name = null;
            if (access.IsAdminOverride)
            {
                var existing = await activities.GetAsync(travelPlanId, activityId, ct);
                name = existing?.Name;
            }

            var ok = await activities.DeleteAsync(travelPlanId, activityId, ct);
            if (!ok)
                throw new ServiceOperationException(404, "Aktivnost nije pronađena.");

            await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Deleted, name ?? "aktivnost", activityId, null, ct);
        }, cancellationToken);

    public Task<ActivityCostSumDto> GetEstimatedCostSumAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var total = await sp.GetRequiredService<ActivitiesDbContext>().TravelActivities.AsNoTracking()
                .Where(a => a.TravelPlanId == travelPlanId)
                .SumAsync(a => (decimal?)a.EstimatedCost, ct) ?? 0m;

            return new ActivityCostSumDto { TotalEstimatedCost = total };
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var deleted = await sp.GetRequiredService<IActivityService>()
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

    private static async Task TryNotifyAsync(
        IServiceProvider sp,
        TravelPlanAccessResolution access,
        ServiceCallContext context,
        Guid travelPlanId,
        AdminMutationAction action,
        string itemLabel,
        Guid? relatedId,
        bool? checklistDone,
        CancellationToken cancellationToken)
    {
        if (!access.IsAdminOverride || context.UserId is not { } adminId)
            return;

        await sp.GetRequiredService<IAdminPlanNotificationService>().NotifyPlanOwnerAsync(
            adminId,
            travelPlanId,
            AdminNotificationCategories.Activity,
            action,
            itemLabel,
            relatedId,
            checklistDone,
            cancellationToken);
    }
}
