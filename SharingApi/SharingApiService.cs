using System.Fabric;
using System.Net.Http;
using CrossService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;
using SharingApi.Data;
using SharingApi.Infrastructure;
using SharingApi.Services;

namespace SharingApi;

internal sealed class SharingApiService : StatefulService, ISharingRemotingService
{
    private readonly IServiceProvider _services;

    public SharingApiService(StatefulServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            services.AddSingleton(context);
            services.AddSingleton(StateManager);
            services.AddSingleton<IReliableStateManager>(StateManager);
            services.AddSingleton<IShareAccessCache, ShareAccessCache>();

            var connectionString = configuration["ConnectionStrings:DefaultConnection"]?.Trim();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection je prazan. Proveri ApplicationParameters (SharingApi_DefaultConnection) ili appsettings.json.");

            services.AddDbContext<SharingDbContext>(options => options.UseSqlServer(connectionString));
            services.AddCrossServiceRemoting();
            services.AddScoped<ISharingService, SharingService>();
        });
    }

    protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() =>
        this.CreateServiceRemotingReplicaListeners();

    public Task<CreateTravelPlanShareLinkResponseDto> CreateShareLinkAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            try
            {
                return await sp.GetRequiredService<ISharingService>()
                    .CreateShareLinkAsync(userId, travelPlanId, request, ct);
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

    public Task<List<SharedTravelPlanListItemDto>> ListSharedPlansAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            try
            {
                return (await sp.GetRequiredService<ISharingService>()
                    .ListSharedPlansForUserAsync(userId, ct)).ToList();
            }
            catch (HttpRequestException)
            {
                throw new ServiceOperationException(
                    503,
                    "Podaci o planovima trenutno nisu dostupni. Proveri da li je pokrenut TravelPlansApi i SharingApi.");
            }
        }, cancellationToken);

    public Task<ClaimShareLinkResponseDto> ClaimShareLinkAsync(
        ServiceCallContext context,
        ClaimShareLinkRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var result = await sp.GetRequiredService<ISharingService>()
                .ClaimShareLinkAsync(userId, request, ct);
            if (result is null)
                throw new ServiceOperationException(404, "Link za deljenje nije pronađen.");
            return result;
        }, cancellationToken);

    public Task<ShareAccessDto> ResolveShareTokenAccessAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var token = context.ShareToken?.Trim();
            if (string.IsNullOrWhiteSpace(token))
                return new ShareAccessDto { Kind = "none" };

            var db = sp.GetRequiredService<SharingDbContext>();
            var cache = sp.GetRequiredService<IShareAccessCache>();

            var tokenHash = ShareTokenCrypto.HashToken(token);
            var cacheKey = $"{tokenHash}:{travelPlanId:D}:{requiresMutation}";
            var cachedKind = await cache.TryGetAsync(cacheKey, ct);
            if (cachedKind is not null)
                return new ShareAccessDto { Kind = cachedKind };

            var now = DateTime.UtcNow;
            var link = await db.TravelPlanShareLinks.AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.TokenHash == tokenHash
                         && l.TravelPlanId == travelPlanId
                         && l.RevokedAtUtc == null
                         && (l.ExpiresAtUtc == null || l.ExpiresAtUtc > now),
                    ct);

            if (link is null)
            {
                await cache.SetAsync(cacheKey, "none", ct);
                return new ShareAccessDto { Kind = "none" };
            }

            var perm = link.Permission.Trim().ToLowerInvariant();
            if (requiresMutation && perm != "edit")
            {
                await cache.SetAsync(cacheKey, "none", ct);
                return new ShareAccessDto { Kind = "none" };
            }

            var kind = perm == "edit" ? "shareEdit" : "shareView";
            await cache.SetAsync(cacheKey, kind, ct);
            return new ShareAccessDto { Kind = kind };
        }, cancellationToken);

    public Task<ShareAccessDto> ResolveRecipientAccessAsync(
        ServiceCallContext context,
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var db = sp.GetRequiredService<SharingDbContext>();

            var recipient = await db.TravelPlanShareRecipients.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TravelPlanId == travelPlanId && r.RecipientUserId == userId, ct);

            if (recipient is null)
                return new ShareAccessDto { Kind = "none" };

            var perm = recipient.Permission.Trim().ToLowerInvariant();
            if (requiresMutation && perm != "edit")
                return new ShareAccessDto { Kind = "none" };

            return new ShareAccessDto { Kind = perm == "edit" ? "shareEdit" : "shareView" };
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var deleted = await sp.GetRequiredService<ISharingService>()
                .DeleteAllByTravelPlanIdAsync(travelPlanId, ct);
            return new CascadeDeleteResultDto { Deleted = deleted };
        }, cancellationToken);
}
