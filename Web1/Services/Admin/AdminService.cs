using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
using ServiceContracts.Dtos;
using Web1.Data;
using Web1.Services.Auth;

namespace Web1.Services.Admin;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;

    public AdminService(AppDbContext db, ITravelPlansInternalClient travelPlans)
    {
        _db = db;
        _travelPlans = travelPlans;
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.RoleId == AuthService.RoleAdminId ? "Admin" : "User",
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<AdminUserListItemDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.RoleId == AuthService.RoleAdminId ? "Admin" : "User",
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error, AdminUserListItemDto? User)> CreateUserAsync(
        CreateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoleId != AuthService.RoleUserId && request.RoleId != AuthService.RoleAdminId)
            return (false, "RoleId mora biti 1 (User) ili 2 (Admin).", null);

        var email = NormalizeEmail(request.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return (false, "Email je već registrovan.", null);

        var user = new Data.Entities.UserEntity
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            RoleId = request.RoleId,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, MapUser(user));
    }

    public async Task<(bool Ok, string? Error, AdminUserListItemDto? User)> UpdateUserAsync(
        Guid actingAdminId,
        Guid targetUserId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoleId is { } rid && rid != AuthService.RoleUserId && rid != AuthService.RoleAdminId)
            return (false, "RoleId mora biti 1 (User) ili 2 (Admin).", null);

        if (!HasAnyUpdate(request))
            return (false, "Pošalji bar jedno polje za izmenu.", null);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null)
            return (false, "Korisnik nije pronađen.", null);

        if (actingAdminId == targetUserId)
        {
            if (request.IsActive == false)
                return (false, "Ne možeš deaktivirati sopstveni nalog.", null);
            if (request.RoleId == AuthService.RoleUserId)
                return (false, "Ne možeš ukloniti Admin ulogu sa sopstvenog naloga.", null);
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = NormalizeEmail(request.Email);
            if (!string.Equals(user.Email, email, StringComparison.Ordinal))
            {
                if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != targetUserId, cancellationToken))
                    return (false, "Email je već registrovan.", null);
                user.Email = email;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11);

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.RoleId.HasValue)
            user.RoleId = request.RoleId.Value;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, MapUser(user));
    }

    public async Task<(bool Ok, string? Error)> DeleteUserAsync(
        Guid actingAdminId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (actingAdminId == targetUserId)
            return (false, "Ne možeš obrisati sopstveni nalog.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null)
            return (false, "Korisnik nije pronađen.");

        try
        {
            await _travelPlans.DeleteAllPlansForUserAsync(targetUserId, cancellationToken);
        }
        catch (ServiceOperationException ex)
        {
            return (false, $"Brisanje planova korisnika nije uspelo. Korisnik nije obrisan. {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Brisanje planova korisnika nije uspelo. Korisnik nije obrisan. {ex.Message}");
        }

        var notifications = await _db.UserNotifications
            .Where(n => n.UserId == targetUserId)
            .ToListAsync(cancellationToken);
        if (notifications.Count > 0)
            _db.UserNotifications.RemoveRange(notifications);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<AdminSystemStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _db.Users.CountAsync(cancellationToken);
        var active = await _db.Users.CountAsync(u => u.IsActive, cancellationToken);
        var admins = await _db.Users.CountAsync(u => u.RoleId == AuthService.RoleAdminId, cancellationToken);

        return new AdminSystemStatsDto
        {
            TotalUsers = total,
            ActiveUsers = active,
            AdminUsers = admins
        };
    }

    private static bool HasAnyUpdate(UpdateAdminUserRequestDto request) =>
        !string.IsNullOrWhiteSpace(request.FirstName)
        || !string.IsNullOrWhiteSpace(request.LastName)
        || !string.IsNullOrWhiteSpace(request.Email)
        || !string.IsNullOrWhiteSpace(request.Password)
        || request.IsActive.HasValue
        || request.RoleId.HasValue;

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AdminUserListItemDto MapUser(Data.Entities.UserEntity user) =>
        new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.RoleId == AuthService.RoleAdminId ? "Admin" : "User",
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
}
