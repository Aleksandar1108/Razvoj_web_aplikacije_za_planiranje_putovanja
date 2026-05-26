using System.Net.Http.Json;
using CrossService.Dtos;
using CrossService.Options;
using Microsoft.Extensions.Options;

namespace CrossService.Clients;

public interface ISharingInternalClient
{
    Task<string> ResolveShareTokenAccessAsync(Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);
    Task<string> ResolveRecipientAccessAsync(Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);
}

public sealed class SharingInternalClient : ISharingInternalClient
{
    private readonly HttpClient _http;
    private readonly MicroserviceUrlsOptions _urls;

    public SharingInternalClient(HttpClient http, IOptions<MicroserviceUrlsOptions> urls)
    {
        _http = http;
        _urls = urls.Value;
    }

    private string Base => _urls.SharingApi.TrimEnd('/');

    public async Task<string> ResolveShareTokenAccessAsync(
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        var url = $"{Base}/api/v1/internal/access/share-token?travelPlanId={travelPlanId:D}&requiresMutation={requiresMutation.ToString().ToLowerInvariant()}";
        var res = await _http.GetAsync(url, cancellationToken);
        if (!res.IsSuccessStatusCode)
            return "none";
        var dto = await res.Content.ReadFromJsonAsync<ShareAccessDto>(cancellationToken);
        return dto?.Kind ?? "none";
    }

    public async Task<string> ResolveRecipientAccessAsync(
        Guid travelPlanId,
        bool requiresMutation,
        CancellationToken cancellationToken)
    {
        var url = $"{Base}/api/v1/internal/access/recipient?travelPlanId={travelPlanId:D}&requiresMutation={requiresMutation.ToString().ToLowerInvariant()}";
        var res = await _http.GetAsync(url, cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound || res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return "none";
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<ShareAccessDto>(cancellationToken);
        return dto?.Kind ?? "none";
    }
}
