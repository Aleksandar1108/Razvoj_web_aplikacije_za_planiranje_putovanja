using System.ComponentModel.DataAnnotations;

namespace Web1.Dtos.Auth;

public sealed class RegisterRequestDto
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
}
