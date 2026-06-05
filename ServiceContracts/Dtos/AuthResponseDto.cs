using System.Runtime.Serialization;

namespace ServiceContracts.Dtos;

[DataContract]
public sealed class AuthResponseDto
{
    [DataMember]
    public string AccessToken { get; set; } = string.Empty;

    [DataMember]
    public DateTime ExpiresAtUtc { get; set; }

    [DataMember]
    public AuthUserDto User { get; set; } = null!;
}

[DataContract]
public sealed class AuthUserDto
{
    [DataMember]
    public Guid Id { get; set; }

    [DataMember]
    public string FirstName { get; set; } = string.Empty;

    [DataMember]
    public string LastName { get; set; } = string.Empty;

    [DataMember]
    public string Email { get; set; } = string.Empty;

    [DataMember]
    public string Role { get; set; } = string.Empty;
}
