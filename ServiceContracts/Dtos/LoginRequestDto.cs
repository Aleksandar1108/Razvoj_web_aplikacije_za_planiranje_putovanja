using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ServiceContracts.Dtos;

[DataContract]
public sealed class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [DataMember]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    [DataMember]
    public string Password { get; set; } = string.Empty;
}
