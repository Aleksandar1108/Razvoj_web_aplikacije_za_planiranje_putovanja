using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public AuthController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.RegisterAsync(request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return MapAuthOperation(result.Value!);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.LoginAsync(request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return MapAuthOperation(result.Value!);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    public Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.GetMeAsync(context, cancellationToken));
    }

    private ActionResult<AuthResponseDto> MapAuthOperation(AuthOperationResultDto operation)
    {
        if (operation.Succeeded && operation.Data is not null)
        {
            if (operation.StatusCode == StatusCodes.Status201Created)
                return StatusCode(StatusCodes.Status201Created, operation.Data);

            return operation.Data;
        }

        return StatusCode(
            operation.StatusCode > 0 ? operation.StatusCode : StatusCodes.Status400BadRequest,
            new { message = operation.ErrorMessage ?? "Greška pri autentikaciji." });
    }
}
