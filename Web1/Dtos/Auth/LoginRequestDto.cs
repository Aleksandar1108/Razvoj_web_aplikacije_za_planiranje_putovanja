using System.ComponentModel.DataAnnotations;

namespace Web1.Dtos.Auth;

public sealed class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
