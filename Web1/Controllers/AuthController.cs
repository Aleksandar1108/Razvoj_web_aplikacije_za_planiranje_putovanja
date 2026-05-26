using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web1.Dtos.Auth;
using Web1.Services.Auth;

namespace Web1.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _auth.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded && result.ErrorCode == AuthErrorCode.Conflict)
            return Conflict(new { message = result.ErrorMessage });

        if (!result.Succeeded && result.ErrorCode == AuthErrorCode.Database)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage });

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _auth.LoginAsync(request, cancellationToken);
        if (!result.Succeeded && result.ErrorCode == AuthErrorCode.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage });

        if (!result.Succeeded && result.ErrorCode == AuthErrorCode.Unauthorized)
            return Unauthorized(new { message = result.ErrorMessage });

        if (!result.Succeeded && result.ErrorCode == AuthErrorCode.Database)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage });

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthUserDto> Me()
    {
        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var userId))
            return Unauthorized();

        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

        return Ok(new AuthUserDto
        {
            Id = userId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        });
    }
}
