using Web1.Dtos.Auth;

namespace Web1.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
