using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ServiceContracts.Dtos;

[DataContract]
public sealed class RegisterRequestDto
{
    [Required]
    [MaxLength(100)]
    [DataMember]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [DataMember]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [DataMember]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera.")]
    [MaxLength(128)]
    [DataMember]
    public string Password { get; set; } = string.Empty;
}
