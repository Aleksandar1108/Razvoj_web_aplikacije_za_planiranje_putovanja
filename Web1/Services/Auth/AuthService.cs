using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Web1.Data;
using Web1.Data.Entities;
using ServiceContracts.Dtos;
using Web1.Options;

namespace Web1.Services.Auth;

public sealed class AuthService : IAuthService
{
    public const byte RoleUserId = 1;
    public const byte RoleAdminId = 2;

    private const string DatabaseUserMessage =
        "Greška pri pristupu bazi podataka. Proveri connection string i da li SQL Server " +
        "dostupan procesu koji pokreće Web1 (LocalDB često ne radi iz Service Fabric servisa — " +
        "koristi punu instancu npr. Server=localhost;Database=...;Trusted_Connection=True;TrustServerCertificate=True).";

    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly IHostEnvironment _env;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwtOptions, IHostEnvironment env)
    {
        _db = db;
        _jwt = jwtOptions.Value;
        _env = env;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        try
        {
            if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
                return AuthResult.Fail("Email je već registrovan.", AuthErrorCode.Conflict);

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
                RoleId = RoleUserId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var roleName = RoleName(user.RoleId);
            var token = CreateToken(user, roleName);
            return AuthResult.Ok(token);
        }
        catch (SqlException ex)
        {
            return AuthResult.Fail(FormatDatabaseError(ex), AuthErrorCode.Database);
        }
        catch (DbUpdateException ex)
        {
            return AuthResult.Fail(FormatDatabaseError(ex.InnerException), AuthErrorCode.Database);
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = NormalizeEmail(request.Email);
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
                return AuthResult.Fail("Pogrešan email ili lozinka.", AuthErrorCode.Unauthorized);

            if (!user.IsActive)
                return AuthResult.Fail("Nalog je deaktiviran.", AuthErrorCode.Forbidden);

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return AuthResult.Fail("Pogrešan email ili lozinka.", AuthErrorCode.Unauthorized);

            var roleName = RoleName(user.RoleId);
            var token = CreateToken(user, roleName);
            return AuthResult.Ok(token);
        }
        catch (SqlException ex)
        {
            return AuthResult.Fail(FormatDatabaseError(ex), AuthErrorCode.Database);
        }
        catch (DbUpdateException ex)
        {
            return AuthResult.Fail(FormatDatabaseError(ex.InnerException), AuthErrorCode.Database);
        }
    }

    private string FormatDatabaseError(Exception? ex)
    {
        if (_env.IsDevelopment() && ex is SqlException sql)
            return $"{DatabaseUserMessage} [SQL {sql.Number}: {sql.Message}]";

        if (_env.IsDevelopment() && ex != null)
            return $"{DatabaseUserMessage} [{ex.GetType().Name}: {ex.Message}]";

        return DatabaseUserMessage;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string RoleName(byte roleId) => roleId == RoleAdminId ? "Admin" : "User";

    private static string DisplayName(UserEntity user) =>
        string.Join(' ', new[] { user.FirstName, user.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()));

    private AuthResponseDto CreateToken(UserEntity user, string roleName)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var display = DisplayName(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new(ClaimTypes.Name, display),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName)
        };

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AuthResponseDto
        {
            AccessToken = tokenString,
            ExpiresAtUtc = expires,
            User = new AuthUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = roleName
            }
        };
    }
}
