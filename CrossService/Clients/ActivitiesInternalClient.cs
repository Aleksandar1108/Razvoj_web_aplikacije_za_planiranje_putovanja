using System.Net.Http.Json;
using CrossService.Dtos;
using CrossService.Options;
using Microsoft.Extensions.Options;

namespace CrossService.Clients;

public interface IActivitiesInternalClient
{
    Task<decimal> GetEstimatedCostSumAsync(Guid travelPlanId, CancellationToken cancellationToken);
}

public sealed class ActivitiesInternalClient : IActivitiesInternalClient
{
    private readonly HttpClient _http;
    private readonly MicroserviceUrlsOptions _urls;

    public ActivitiesInternalClient(HttpClient http, IOptions<MicroserviceUrlsOptions> urls)
    {
        _http = http;
        _urls = urls.Value;
    }

    private string Base => _urls.ActivitiesApi.TrimEnd('/');

    public async Task<decimal> GetEstimatedCostSumAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var res = await _http.GetAsync(
            $"{Base}/api/v1/internal/travel-plans/{travelPlanId:D}/activities/estimated-cost-sum",
            cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return 0m;
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<ActivityCostSumDto>(cancellationToken);
        return dto?.TotalEstimatedCost ?? 0m;
    }
}
