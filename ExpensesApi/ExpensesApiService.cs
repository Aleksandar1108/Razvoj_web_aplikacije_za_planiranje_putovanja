using System.Fabric;
using CrossService;
using CrossService.Access;
using ExpensesApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;

namespace ExpensesApi;

internal sealed class ExpensesApiService : StatelessService, IExpensesRemotingService
{
    private readonly IServiceProvider _services;

    public ExpensesApiService(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<Data.ExpensesDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<List<TravelExpenseResponseDto>> ListExpensesAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            return (await sp.GetRequiredService<IExpenseService>().ListByTravelPlanIdAsync(travelPlanId, ct)).ToList();
        }, cancellationToken);

    public Task<ExpenseSummaryDto> GetExpenseSummaryAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<IExpenseService>().GetSummaryAsync(travelPlanId, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");
            return dto;
        }, cancellationToken);

    public Task<TravelExpenseResponseDto> GetExpenseAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<IExpenseService>().GetAsync(travelPlanId, expenseId, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Trošak nije pronađen.");
            return dto;
        }, cancellationToken);

    public Task<TravelExpenseResponseDto> CreateExpenseAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CreateTravelExpenseRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var created = await sp.GetRequiredService<IExpenseService>().CreateAsync(travelPlanId, request, ct);
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

    public Task<TravelExpenseResponseDto> UpdateExpenseAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid expenseId,
        UpdateTravelExpenseRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            try
            {
                var updated = await sp.GetRequiredService<IExpenseService>()
                    .UpdateAsync(travelPlanId, expenseId, request, ct);
                if (updated is null)
                    throw new ServiceOperationException(404, "Trošak nije pronađen.");

                await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Updated, updated.Name, updated.Id, null, ct);
                return updated;
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task DeleteExpenseAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var access = await RequireAllowedAccess(sp, context, travelPlanId, requiresMutation: true, ct);
            RequireMutate(access);

            var expenses = sp.GetRequiredService<IExpenseService>();
            string? name = null;
            if (access.IsAdminOverride)
            {
                var existing = await expenses.GetAsync(travelPlanId, expenseId, ct);
                name = existing?.Name;
            }

            var ok = await expenses.DeleteAsync(travelPlanId, expenseId, ct);
            if (!ok)
                throw new ServiceOperationException(404, "Trošak nije pronađen.");

            await TryNotifyAsync(sp, access, context, travelPlanId, AdminMutationAction.Deleted, name ?? "trosak", expenseId, null, ct);
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var deleted = await sp.GetRequiredService<IExpenseService>()
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
            AdminNotificationCategories.Expense,
            action,
            itemLabel,
            relatedId,
            checklistDone,
            cancellationToken);
    }
}
