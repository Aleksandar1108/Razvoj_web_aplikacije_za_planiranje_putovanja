using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ServiceContracts;

namespace ApiGateway.Infrastructure;

public static class ServiceCallContextFactory
{
    public const string ShareTokenHeaderName = "X-Share-Token";

    public static ServiceCallContext FromHttpContext(HttpContext httpContext)
    {
        var shareToken = httpContext.Request.Headers.TryGetValue(ShareTokenHeaderName, out var shareValues)
            ? shareValues.FirstOrDefault()?.Trim()
            : null;

        Guid? userId = null;
        string? role = null;
        if (httpContext.User.Identity?.IsAuthenticated == true && TryGetUserId(httpContext.User, out var parsedUserId))
        {
            userId = parsedUserId;
            role = httpContext.User.FindFirstValue(ClaimTypes.Role);
        }

        if (!string.IsNullOrWhiteSpace(shareToken))
        {
            return new ServiceCallContext
            {
                UserId = userId,
                Role = role,
                ShareToken = shareToken
            };
        }

        if (userId is null)
            return ServiceCallContext.Anonymous;

        return ServiceCallContext.ForUser(userId.Value, role);
    }

    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    public static Guid? GetUserId(ClaimsPrincipal user) =>
        TryGetUserId(user, out var id) ? id : null;
}
