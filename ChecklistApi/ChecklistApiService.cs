using System.Fabric;
using ChecklistApi.Services;
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

namespace ChecklistApi;

internal sealed class ChecklistApiService : StatelessService, IChecklistRemotingService
{
    private readonly IServiceProvider _services;

    public ChecklistApiService(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<Data.ChecklistDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddScoped<IChecklistService, ChecklistService>();
            services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<List<ChecklistItemResponseDto>> ListChecklistItemsAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            return (await sp.GetRequiredService<IChecklistService>().ListByTravelPlanIdAsync(travelPlanId, ct)).ToList();
        }, cancellationToken);

    public Task<ChecklistItemResponseDto> CreateChecklistItemAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CreateChecklistItemRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var created = await sp.GetRequiredService<IChecklistService>().CreateAsync(travelPlanId, request, ct);
                await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Created, created.Title, created.Id, null, ct);
                return created;
            }
            catch (InvalidOperationException)
            {
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");
            }
        }, cancellationToken);

    public Task<ChecklistItemResponseDto> GetChecklistItemAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<IChecklistService>().GetAsync(travelPlanId, itemId, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Stavka nije pronađena.");
            return dto;
        }, cancellationToken);

    public Task<ChecklistItemResponseDto> UpdateChecklistItemAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid itemId,
        UpdateChecklistItemRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var updated = await sp.GetRequiredService<IChecklistService>().UpdateAsync(travelPlanId, itemId, request, ct);
            if (updated is null)
                throw new ServiceOperationException(404, "Stavka nije pronađena.");

            await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Updated, updated.Title, updated.Id, null, ct);
            return updated;
        }, cancellationToken);

    public Task<ChecklistItemResponseDto> ToggleChecklistItemAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid itemId,
        ToggleChecklistItemRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var updated = await sp.GetRequiredService<IChecklistService>().ToggleAsync(travelPlanId, itemId, request.IsDone, ct);
            if (updated is null)
                throw new ServiceOperationException(404, "Stavka nije pronađena.");

            await TryNotifyAsync(
                sp,
                access,
                context,
                travelPlanId,
                AdminMutationAction.Toggled,
                updated.Title,
                updated.Id,
                updated.IsDone,
                ct);
            return updated;
        }, cancellationToken);

    public Task DeleteChecklistItemAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var checklist = sp.GetRequiredService<IChecklistService>();
            string? title = null;
            if (access.IsAdminOverride)
            {
                var existing = await checklist.GetAsync(travelPlanId, itemId, ct);
                title = existing?.Title;
            }

            var ok = await checklist.DeleteAsync(travelPlanId, itemId, ct);
            if (!ok)
                throw new ServiceOperationException(404, "Stavka nije pronađena.");

            await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Deleted, title ?? "stavku", itemId, null, ct);
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var deleted = await sp.GetRequiredService<IChecklistService>()
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
            AdminNotificationCategories.Checklist,
            action,
            itemLabel,
            relatedId,
            checklistDone,
            cancellationToken);
    }
}
