using CrossService.Access;
using Microsoft.AspNetCore.Http;

namespace CrossService.Http;

public sealed class ForwardAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
        {
            var auth = ctx.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth))
                request.Headers.TryAddWithoutValidation("Authorization", auth);

            if (ctx.Request.Headers.TryGetValue(ITravelPlanAccessGuard.ShareTokenHeaderName, out var share))
            {
                var token = share.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.TryAddWithoutValidation(ITravelPlanAccessGuard.ShareTokenHeaderName, token);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
