using ServiceContracts.Dtos;

namespace Web1.Services.Auth;

public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public AuthResponseDto? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public AuthErrorCode? ErrorCode { get; init; }

    public static AuthResult Ok(AuthResponseDto data) =>
        new() { Succeeded = true, Data = data };

    public static AuthResult Fail(string message, AuthErrorCode code) =>
        new() { Succeeded = false, ErrorMessage = message, ErrorCode = code };
}

public enum AuthErrorCode
{
    Conflict,
    Unauthorized,
    Forbidden,
    Validation,
    Database
}
