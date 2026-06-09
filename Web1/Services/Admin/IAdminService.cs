using ServiceContracts.Dtos;

namespace Web1.Services.Admin;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<AdminUserListItemDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, AdminUserListItemDto? User)> CreateUserAsync(
        CreateAdminUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, AdminUserListItemDto? User)> UpdateUserAsync(
        Guid actingAdminId,
        Guid targetUserId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AdminSystemStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteUserAsync(
        Guid actingAdminId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);
}
