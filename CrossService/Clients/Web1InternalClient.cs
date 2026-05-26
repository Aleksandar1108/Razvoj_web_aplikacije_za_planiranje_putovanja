using System.Net.Http.Json;
using CrossService.Dtos;
using CrossService.Options;
using Microsoft.Extensions.Options;

namespace CrossService.Clients;

public interface IWeb1InternalClient
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, UserBriefDto>> GetUsersBriefAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);
    Task CreateAdminNotificationAsync(CreateAdminNotificationRequestDto request, CancellationToken cancellationToken);
}

public sealed class Web1InternalClient : IWeb1InternalClient
{
    private readonly HttpClient _http;
    private readonly MicroserviceUrlsOptions _urls;

    public Web1InternalClient(HttpClient http, IOptions<MicroserviceUrlsOptions> urls)
    {
        _http = http;
        _urls = urls.Value;
    }

    private string Base => _urls.Web1.TrimEnd('/');

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var res = await _http.GetAsync($"{Base}/api/v1/internal/users/{userId:D}/exists", cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<UserExistsDto>(cancellationToken);
        return dto?.Exists ?? false;
    }

    public async Task<IReadOnlyDictionary<Guid, UserBriefDto>> GetUsersBriefAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, UserBriefDto>();

        var res = await _http.PostAsJsonAsync(
            $"{Base}/api/v1/internal/users/brief",
            new UsersBriefRequestDto { UserIds = userIds.Distinct().ToList() },
            cancellationToken);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<UserBriefDto>>(cancellationToken) ?? new List<UserBriefDto>();
        return list.ToDictionary(u => u.Id);
    }

    public async Task CreateAdminNotificationAsync(
        CreateAdminNotificationRequestDto request,
        CancellationToken cancellationToken)
    {
        var res = await _http.PostAsJsonAsync($"{Base}/api/v1/internal/notifications", request, cancellationToken);
        res.EnsureSuccessStatusCode();
    }
}
