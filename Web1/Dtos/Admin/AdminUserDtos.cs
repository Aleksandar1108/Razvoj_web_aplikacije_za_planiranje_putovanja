using System.ComponentModel.DataAnnotations;

namespace Web1.Dtos.Admin;

public sealed class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AdminSystemStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
}

public sealed class UpdateAdminUserRequestDto
{
    public bool? IsActive { get; set; }

    [Range(1, 2)]
    public byte? RoleId { get; set; }
}
