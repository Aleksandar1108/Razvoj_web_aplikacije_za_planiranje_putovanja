using System.Runtime.Serialization;

namespace ServiceContracts.Dtos;

[DataContract]
public sealed class AuthOperationResultDto
{
    [DataMember]
    public bool Succeeded { get; set; }

    [DataMember]
    public AuthResponseDto? Data { get; set; }

    [DataMember]
    public string? ErrorMessage { get; set; }

    [DataMember]
    public int StatusCode { get; set; }
}
