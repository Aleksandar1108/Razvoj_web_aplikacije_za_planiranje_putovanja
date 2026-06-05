using System.Fabric;
using System.Net.Http;
using CrossService;
using CrossService.Access;
using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;
using TravelPlansApi.Data;
using TravelPlansApi.Infrastructure;
using TravelPlansApi.Services;

namespace TravelPlansApi;

internal sealed class TravelPlansApiService : StatelessService, ITravelPlansRemotingService
{
    private readonly IServiceProvider _services;

    public TravelPlansApiService(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<TravelPlansDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddPlanCascadeDeleteClient();
            services.AddScoped<ITravelPlanAccessGuard, TravelPlanAccessGuard>();
            services.AddScoped<ITravelPlanService, TravelPlanService>();
            services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<List<TravelPlanResponseDto>> ListTravelPlansAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            return (await sp.GetRequiredService<ITravelPlanService>().ListForUserAsync(userId, ct)).ToList();
        }, cancellationToken);

    public Task<TravelPlanResponseDto> GetTravelPlanAsync(
        ServiceCallContext context,
        Guid id,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            await RequireAllowedAccess(sp, context, id, requiresMutation: false, ct);
            var dto = await sp.GetRequiredService<ITravelPlanService>().GetByIdAsync(id, ct);
            if (dto is null)
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");
            return dto;
        }, cancellationToken);

    public Task<TravelPlanResponseDto> CreateTravelPlanAsync(
        ServiceCallContext context,
        CreateTravelPlanRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            try
            {
                return await sp.GetRequiredService<ITravelPlanService>().CreateAsync(userId, request, ct);
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task<TravelPlanResponseDto> UpdateTravelPlanAsync(
        ServiceCallContext context,
        Guid id,
        UpdateTravelPlanRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var access = await RequireAllowedAccess(sp, context, id, requiresMutation: true, ct);
            RequireMutate(access);

            var plans = sp.GetRequiredService<ITravelPlanService>();
            var notifications = sp.GetRequiredService<IAdminPlanNotificationService>();
            var before = access.IsAdminOverride ? await plans.GetByIdAsync(id, ct) : null;

            try
            {
                var updated = access.IsAdminOverride
                    ? await plans.UpdateByPlanIdAsync(id, request, ct)
                    : await plans.UpdateAsync(userId, id, request, ct);

                if (updated is null)
                    throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");

                if (access.IsAdminOverride && before is not null)
                    await NotifyPlanUpdateAsync(notifications, userId, before, updated, ct);

                return updated;
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task DeleteTravelPlanAsync(
        ServiceCallContext context,
        Guid id,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var access = await RequireAllowedAccess(sp, context, id, requiresMutation: true, ct);
            RequireMutate(access);

            var plans = sp.GetRequiredService<ITravelPlanService>();
            var notifications = sp.GetRequiredService<IAdminPlanNotificationService>();

            string? planName = null;
            if (access.IsAdminOverride)
            {
                var before = await plans.GetByIdAsync(id, ct);
                planName = before?.Name;
            }

            try
            {
                var ok = access.IsAdminOverride
                    ? await plans.DeleteByPlanIdAsync(id, ct)
                    : await plans.DeleteAsync(userId, id, ct);

                if (!ok)
                    throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceOperationException(
                    503,
                    $"Brisanje povezanih podataka nije uspelo. Plan nije obrisan. {ex.Message}");
            }

            if (access.IsAdminOverride)
            {
                await TryNotifyAdminAsync(
                    notifications,
                    userId,
                    access,
                    id,
                    AdminNotificationCategories.PlanBasic,
                    AdminMutationAction.Deleted,
                    planName,
                    null,
                    ct);
            }
        }, cancellationToken);

    public Task<List<AdminTravelPlanListItemDto>> ListAdminTravelPlansAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            var db = sp.GetRequiredService<TravelPlansDbContext>();
            var web1 = sp.GetRequiredService<IWeb1InternalClient>();

            var plans = await db.TravelPlans.AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            var userIds = plans.Select(p => p.UserId).Distinct().ToList();
            var users = await web1.GetUsersBriefAsync(userIds, ct);

            List<AdminTravelPlanListItemDto> rows = plans.Select(p =>
            {
                users.TryGetValue(p.UserId, out var user);
                return new AdminTravelPlanListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    OwnerUserId = p.UserId,
                    OwnerEmail = user?.Email ?? string.Empty,
                    OwnerDisplayName = user?.DisplayName ?? "Korisnik",
                    StartDate = DateContract.FromDateOnly(p.StartDate),
                    EndDate = DateContract.FromDateOnly(p.EndDate)
                };
            }).ToList();
            return rows;
        }, cancellationToken);

    public Task<TravelPlanResponseDto> AdminCreateTravelPlanAsync(
        ServiceCallContext context,
        AdminCreateTravelPlanRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var adminId = RemotingAuth.RequireUserId(context);
            RemotingAuth.RequireAdmin(context);

            var web1 = sp.GetRequiredService<IWeb1InternalClient>();
            var plans = sp.GetRequiredService<ITravelPlanService>();
            var notifications = sp.GetRequiredService<IAdminPlanNotificationService>();

            if (!await web1.UserExistsAsync(request.OwnerUserId, ct))
                throw new ServiceOperationException(404, "Korisnik nije pronađen.");

            var createDto = new CreateTravelPlanRequestDto
            {
                Name = request.Name,
                ShortDescription = request.ShortDescription,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PlannedBudget = request.PlannedBudget,
                GeneralNotes = request.GeneralNotes
            };

            try
            {
                var created = await plans.CreateAsync(request.OwnerUserId, createDto, ct);

                if (request.OwnerUserId != adminId)
                {
                    await notifications.NotifyPlanOwnerAsync(
                        adminId,
                        created.Id,
                        AdminNotificationCategories.PlanBasic,
                        AdminMutationAction.Created,
                        cancellationToken: ct);
                }

                return created;
            }
            catch (ArgumentException ex)
            {
                throw new ServiceOperationException(400, ex.Message);
            }
        }, cancellationToken);

    public Task<TravelPlanMetaDto> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var p = await sp.GetRequiredService<TravelPlansDbContext>().TravelPlans.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == travelPlanId, ct);
            if (p is null)
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");

            return new TravelPlanMetaDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                StartDate = DateContract.FromDateOnly(p.StartDate),
                EndDate = DateContract.FromDateOnly(p.EndDate),
                PlannedBudget = p.PlannedBudget
            };
        }, cancellationToken);

    public Task<TravelPlanExistsDto> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var exists = await sp.GetRequiredService<TravelPlansDbContext>().TravelPlans.AsNoTracking()
                .AnyAsync(x => x.Id == travelPlanId, ct);
            return new TravelPlanExistsDto { Exists = exists };
        }, cancellationToken);

    public Task<TravelPlanOwnerDto> GetOwnerAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var p = await sp.GetRequiredService<TravelPlansDbContext>().TravelPlans.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == travelPlanId, ct);
            if (p is null)
                return new TravelPlanOwnerDto { IsOwner = false };

            return new TravelPlanOwnerDto
            {
                IsOwner = p.UserId == userId,
                OwnerUserId = p.UserId
            };
        }, cancellationToken);

    public Task<List<TravelPlanMetaDto>> GetMetaBatchAsync(
        ServiceCallContext context,
        SharedPlanMetaBatchRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireUserId(context);

            var ids = request.TravelPlanIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new List<TravelPlanMetaDto>();

            var rows = await sp.GetRequiredService<TravelPlansDbContext>().TravelPlans.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct);

            return rows.Select(p => new TravelPlanMetaDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                StartDate = DateContract.FromDateOnly(p.StartDate),
                EndDate = DateContract.FromDateOnly(p.EndDate),
                PlannedBudget = p.PlannedBudget
            }).ToList();
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

    private static async Task NotifyPlanUpdateAsync(
        IAdminPlanNotificationService notifications,
        Guid adminId,
        TravelPlanResponseDto before,
        TravelPlanResponseDto after,
        CancellationToken cancellationToken)
    {
        var access = new TravelPlanAccessResolution(TravelPlanAccessKind.Admin);
        var notesChanged = !string.Equals(
            before.GeneralNotes?.Trim() ?? string.Empty,
            after.GeneralNotes?.Trim() ?? string.Empty,
            StringComparison.Ordinal);

        var basicChanged =
            before.Name != after.Name
            || before.ShortDescription != after.ShortDescription
            || before.StartDate != after.StartDate
            || before.EndDate != after.EndDate
            || before.PlannedBudget != after.PlannedBudget;

        if (basicChanged)
        {
            await TryNotifyAdminAsync(
                notifications,
                adminId,
                access,
                after.Id,
                AdminNotificationCategories.PlanBasic,
                AdminMutationAction.Updated,
                null,
                null,
                cancellationToken);
        }

        if (notesChanged)
        {
            await TryNotifyAdminAsync(
                notifications,
                adminId,
                access,
                after.Id,
                AdminNotificationCategories.PlanNotes,
                AdminMutationAction.Updated,
                null,
                null,
                cancellationToken);
        }
    }

    private static async Task TryNotifyAdminAsync(
        IAdminPlanNotificationService notifications,
        Guid adminId,
        TravelPlanAccessResolution access,
        Guid travelPlanId,
        string category,
        AdminMutationAction action,
        string? itemLabel,
        Guid? relatedId,
        CancellationToken cancellationToken)
    {
        if (!access.IsAdminOverride)
            return;

        await notifications.NotifyPlanOwnerAsync(
            adminId,
            travelPlanId,
            category,
            action,
            itemLabel,
            relatedId,
            null,
            cancellationToken);
    }
}
