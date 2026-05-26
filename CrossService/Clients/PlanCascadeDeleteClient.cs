using CrossService.Options;
using Microsoft.Extensions.Options;

namespace CrossService.Clients;

public interface IPlanCascadeDeleteClient
{
    Task DeleteAllRelatedDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}

public sealed class PlanCascadeDeleteClient : IPlanCascadeDeleteClient
{
    private readonly HttpClient _http;
    private readonly MicroserviceUrlsOptions _urls;

    public PlanCascadeDeleteClient(HttpClient http, IOptions<MicroserviceUrlsOptions> urls)
    {
        _http = http;
        _urls = urls.Value;
    }

    public async Task DeleteAllRelatedDataAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var planId = travelPlanId.ToString("D");
        var failures = new List<string>();

        await Task.WhenAll(
            TryDeleteAsync(_urls.DestinationsApi, "DestinationsApi", planId, failures, cancellationToken),
            TryDeleteAsync(_urls.ActivitiesApi, "ActivitiesApi", planId, failures, cancellationToken),
            TryDeleteAsync(_urls.ExpensesApi, "ExpensesApi", planId, failures, cancellationToken),
            TryDeleteAsync(_urls.ChecklistApi, "ChecklistApi", planId, failures, cancellationToken),
            TryDeleteAsync(_urls.SharingApi, "SharingApi", planId, failures, cancellationToken),
            TryDeleteAsync(_urls.Web1, "Web1", planId, failures, cancellationToken));

        if (failures.Count > 0)
            throw new HttpRequestException(string.Join(" ", failures));
    }

    private async Task TryDeleteAsync(
        string baseUrl,
        string serviceName,
        string planId,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        var root = baseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root))
        {
            lock (failures)
            {
                failures.Add($"{serviceName}: URL nije podešen.");
            }
            return;
        }

        try
        {
            var res = await _http.DeleteAsync(
                $"{root}/api/v1/internal/travel-plans/{planId}/cascade",
                cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                lock (failures)
                {
                    failures.Add($"{serviceName} ({(int)res.StatusCode}): {body}");
                }
            }
        }
        catch (Exception ex)
        {
            lock (failures)
            {
                failures.Add($"{serviceName}: {ex.Message}");
            }
        }
    }
}
