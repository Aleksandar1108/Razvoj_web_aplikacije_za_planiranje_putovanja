using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.Dtos;

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

public sealed class CreateAdminUserRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Range(1, 2)]
    public byte RoleId { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateAdminUserRequestDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera.")]
    [MaxLength(128)]
    public string? Password { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, 2)]
    public byte? RoleId { get; set; }
}
