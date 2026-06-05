using System.Fabric;
using CrossService;
using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceContracts;
using ServiceContracts.Dtos;
using ServiceContracts.Remoting;
using Web1.Data;
using Web1.Data.Entities;
using Web1.Infrastructure;
using Web1.Options;
using Web1.Services.Admin;
using Web1.Services.Auth;
using Web1.Services.Notifications;

namespace Web1;

internal sealed class Web1 : StatelessService, IWeb1RemotingService
{
    private readonly IServiceProvider _services;

    public Web1(StatelessServiceContext context)
        : base(context)
    {
        _services = ServiceHostBootstrap.BuildProvider((services, configuration) =>
        {
            var connectionString = ServiceHostBootstrap.RequireConnectionString(configuration);

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            var signingKey = configuration[$"{JwtOptions.SectionName}:SigningKey"]?.Trim();
            if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:SigningKey u Web1 mora imati najmanje 32 karaktera. Proveri da li se appsettings.json kopira u build.");
            services.AddSingleton<IHostEnvironment, ServiceFabricHostEnvironment>();
            services.AddCrossServiceRemoting();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<INotificationService, NotificationService>();
        });
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() =>
        this.CreateServiceRemotingInstanceListeners();

    public Task<AuthOperationResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var auth = sp.GetRequiredService<IAuthService>();
            var result = await auth.RegisterAsync(request, ct);
            return MapAuthResult(result, successStatusCode: 201);
        }, cancellationToken);

    public Task<AuthOperationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var auth = sp.GetRequiredService<IAuthService>();
            var result = await auth.LoginAsync(request, ct);
            return MapAuthResult(result, successStatusCode: 200);
        }, cancellationToken);

    public Task<AuthUserDto> GetMeAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, ct)
                ?? throw new ServiceOperationException(401, "Niste prijavljeni.");

            return new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role?.Name ?? "User"
            };
        }, cancellationToken);

    public Task<AdminSystemStatsDto> GetAdminStatsAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            return await sp.GetRequiredService<IAdminService>().GetStatsAsync(ct);
        }, cancellationToken);

    public Task<List<AdminUserListItemDto>> ListAdminUsersAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            return (await sp.GetRequiredService<IAdminService>().ListUsersAsync(ct)).ToList();
        }, cancellationToken);

    public Task<AdminUserListItemDto> GetAdminUserAsync(ServiceCallContext context, Guid userId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            RemotingAuth.RequireAdmin(context);
            var user = await sp.GetRequiredService<IAdminService>().GetUserAsync(userId, ct);
            if (user is null)
                throw new ServiceOperationException(404, "Korisnik nije pronađen.");
            return user;
        }, cancellationToken);

    public Task<AdminUserListItemDto> UpdateAdminUserAsync(
        ServiceCallContext context,
        Guid userId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var adminId = RemotingAuth.RequireUserId(context);
            RemotingAuth.RequireAdmin(context);
            var (ok, error, updated) = await sp.GetRequiredService<IAdminService>()
                .UpdateUserAsync(adminId, userId, request, ct);
            if (!ok && error == "Korisnik nije pronađen.")
                throw new ServiceOperationException(404, error);
            if (!ok)
                throw new ServiceOperationException(400, error ?? "Greška pri ažuriranju.");
            return updated!;
        }, cancellationToken);

    public Task<List<UserNotificationDto>> ListNotificationsAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            return (await sp.GetRequiredService<INotificationService>().ListForUserAsync(userId, ct)).ToList();
        }, cancellationToken);

    public Task<UnreadNotificationCountDto> GetUnreadNotificationCountAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var count = await sp.GetRequiredService<INotificationService>().GetUnreadCountAsync(userId, ct);
            return new UnreadNotificationCountDto { Count = count };
        }, cancellationToken);

    public Task MarkNotificationReadAsync(ServiceCallContext context, Guid notificationId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            var ok = await sp.GetRequiredService<INotificationService>().MarkReadAsync(userId, notificationId, ct);
            if (!ok)
                throw new ServiceOperationException(404, "Obaveštenje nije pronađeno.");
        }, cancellationToken);

    public Task MarkAllNotificationsReadAsync(ServiceCallContext context, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var userId = RemotingAuth.RequireUserId(context);
            await sp.GetRequiredService<INotificationService>().MarkAllReadAsync(userId, ct);
        }, cancellationToken);

    public Task<UserExistsDto> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var exists = await sp.GetRequiredService<AppDbContext>().Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId, ct);
            return new UserExistsDto { Exists = exists };
        }, cancellationToken);

    public Task<List<UserBriefDto>> GetUsersBriefAsync(UsersBriefRequestDto request, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var ids = request.UserIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new List<UserBriefDto>();

            var users = await sp.GetRequiredService<AppDbContext>().Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new UserBriefDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    DisplayName = DisplayName(u.FirstName, u.LastName)
                })
                .ToListAsync(ct);
            return users;
        }, cancellationToken);

    public Task CreateAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var travelPlans = sp.GetRequiredService<ITravelPlansInternalClient>();
            var db = sp.GetRequiredService<AppDbContext>();
            var meta = await travelPlans.GetMetaAsync(request.TravelPlanId, ct);
            if (meta is null)
                throw new ServiceOperationException(404, "Plan putovanja nije pronađen.");

            var planName = string.IsNullOrWhiteSpace(meta.Name) ? "putovanje" : meta.Name.Trim();
            var (title, message) = AdminNotificationMessageBuilder.Build(
                request.Category,
                request.Action,
                planName,
                request.ItemLabel,
                request.ChecklistDone);

            db.UserNotifications.Add(new UserNotificationEntity
            {
                Id = Guid.NewGuid(),
                UserId = meta.UserId,
                Category = request.Category,
                Title = title,
                Message = message,
                TravelPlanId = request.TravelPlanId,
                TravelDestinationId = request.RelatedEntityId,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }, cancellationToken);

    public Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        RemotingScope.ExecuteAsync(_services, async (sp, ct) =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var rows = await db.UserNotifications
                .Where(n => n.TravelPlanId == travelPlanId)
                .ToListAsync(ct);
            if (rows.Count == 0)
                return new CascadeDeleteResultDto { Deleted = 0 };

            db.UserNotifications.RemoveRange(rows);
            await db.SaveChangesAsync(ct);
            return new CascadeDeleteResultDto { Deleted = rows.Count };
        }, cancellationToken);

    private static AuthOperationResultDto MapAuthResult(AuthResult result, int successStatusCode)
    {
        if (result.Succeeded)
        {
            return new AuthOperationResultDto
            {
                Succeeded = true,
                Data = result.Data,
                StatusCode = successStatusCode
            };
        }

        return new AuthOperationResultDto
        {
            Succeeded = false,
            ErrorMessage = result.ErrorMessage ?? "Greška pri autentikaciji.",
            StatusCode = MapAuthStatusCode(result.ErrorCode)
        };
    }

    private static int MapAuthStatusCode(AuthErrorCode? errorCode) =>
        errorCode switch
        {
            AuthErrorCode.Conflict => 409,
            AuthErrorCode.Unauthorized => 401,
            AuthErrorCode.Forbidden => 403,
            AuthErrorCode.Database => 503,
            _ => 400
        };

    private static string DisplayName(string first, string last)
    {
        var parts = new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim());
        var name = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(name) ? "Korisnik" : name;
    }
}
