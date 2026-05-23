using Microsoft.EntityFrameworkCore;
using Web1.Data;
using Web1.Data.Entities;
using Web1.Dtos.Admin;
using Web1.Services.Auth;

namespace Web1.Services.Admin;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
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

    public async Task<(bool Ok, string? Error, AdminUserListItemDto? User)> UpdateUserAsync(
        Guid actingAdminId,
        Guid targetUserId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoleId is { } rid && rid != AuthService.RoleUserId && rid != AuthService.RoleAdminId)
            return (false, "RoleId mora biti 1 (User) ili 2 (Admin).", null);

        if (request.IsActive is null && request.RoleId is null)
            return (false, "Pošalji IsActive i/ili RoleId.", null);

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

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.RoleId.HasValue)
            user.RoleId = request.RoleId.Value;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new AdminUserListItemDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.RoleId == AuthService.RoleAdminId ? "Admin" : "User",
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return (true, null, dto);
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
}
