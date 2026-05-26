using System.Net.Http.Json;
using CrossService.Dtos;
using CrossService.Options;
using Microsoft.Extensions.Options;

namespace CrossService.Clients;

public interface ITravelPlansInternalClient
{
    Task<TravelPlanMetaDto?> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelPlanOwnerDto> GetOwnerAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelPlanMetaDto>> GetMetaBatchAsync(IReadOnlyList<Guid> travelPlanIds, CancellationToken cancellationToken);
}

public sealed class TravelPlansInternalClient : ITravelPlansInternalClient
{
    private readonly HttpClient _http;
    private readonly MicroserviceUrlsOptions _urls;

    public TravelPlansInternalClient(HttpClient http, IOptions<MicroserviceUrlsOptions> urls)
    {
        _http = http;
        _urls = urls.Value;
    }

    private string Base => _urls.TravelPlansApi.TrimEnd('/');

    public async Task<TravelPlanMetaDto?> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var res = await _http.GetAsync($"{Base}/api/v1/internal/travel-plans/{travelPlanId:D}/meta", cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TravelPlanMetaDto>(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var res = await _http.GetAsync($"{Base}/api/v1/internal/travel-plans/{travelPlanId:D}/exists", cancellationToken);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<TravelPlanExistsDto>(cancellationToken);
        return dto?.Exists ?? false;
    }

    public async Task<TravelPlanOwnerDto> GetOwnerAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var res = await _http.GetAsync($"{Base}/api/v1/internal/travel-plans/{travelPlanId:D}/owner", cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TravelPlanOwnerDto>(cancellationToken)
               ?? new TravelPlanOwnerDto();
    }

    public async Task<IReadOnlyList<TravelPlanMetaDto>> GetMetaBatchAsync(
        IReadOnlyList<Guid> travelPlanIds,
        CancellationToken cancellationToken)
    {
        if (travelPlanIds.Count == 0)
            return Array.Empty<TravelPlanMetaDto>();

        var res = await _http.PostAsJsonAsync(
            $"{Base}/api/v1/internal/travel-plans/meta-batch",
            new SharedPlanMetaBatchRequestDto { TravelPlanIds = travelPlanIds.ToList() },
            cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"TravelPlansApi meta-batch nije uspeo ({(int)res.StatusCode}): {body}");
        }

        return await res.Content.ReadFromJsonAsync<List<TravelPlanMetaDto>>(cancellationToken)
               ?? new List<TravelPlanMetaDto>();
    }
}
