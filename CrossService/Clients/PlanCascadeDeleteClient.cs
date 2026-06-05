using ServiceContracts.Remoting;

namespace CrossService.Clients;

public interface IPlanCascadeDeleteClient
{
    Task DeleteAllRelatedDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}

public sealed class PlanCascadeDeleteClient : IPlanCascadeDeleteClient
{
    private readonly IDestinationsRemotingService _destinations;
    private readonly IActivitiesRemotingService _activities;
    private readonly IExpensesRemotingService _expenses;
    private readonly IChecklistRemotingService _checklist;
    private readonly ISharingRemotingService _sharing;
    private readonly IWeb1RemotingService _web1;

    public PlanCascadeDeleteClient()
    {
        _destinations = ServiceFabricRemoting.CreateProxy<IDestinationsRemotingService>(ServiceFabricRemoting.ServiceNames.DestinationsApi);
        _activities = ServiceFabricRemoting.CreateProxy<IActivitiesRemotingService>(ServiceFabricRemoting.ServiceNames.ActivitiesApi);
        _expenses = ServiceFabricRemoting.CreateProxy<IExpensesRemotingService>(ServiceFabricRemoting.ServiceNames.ExpensesApi);
        _checklist = ServiceFabricRemoting.CreateProxy<IChecklistRemotingService>(ServiceFabricRemoting.ServiceNames.ChecklistApi);
        _sharing = ServiceFabricRemoting.CreateProxy<ISharingRemotingService>(ServiceFabricRemoting.ServiceNames.SharingApi);
        _web1 = ServiceFabricRemoting.CreateProxy<IWeb1RemotingService>(ServiceFabricRemoting.ServiceNames.Web1);
    }

    public async Task DeleteAllRelatedDataAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        await Task.WhenAll(
            TryDeleteAsync(() => _destinations.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "DestinationsApi", failures),
            TryDeleteAsync(() => _activities.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "ActivitiesApi", failures),
            TryDeleteAsync(() => _expenses.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "ExpensesApi", failures),
            TryDeleteAsync(() => _checklist.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "ChecklistApi", failures),
            TryDeleteAsync(() => _sharing.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "SharingApi", failures),
            TryDeleteAsync(() => _web1.CascadeDeleteTravelPlanDataAsync(travelPlanId, cancellationToken), "Web1", failures));

        if (failures.Count > 0)
            throw new HttpRequestException(string.Join(" ", failures));
    }

    private static async Task TryDeleteAsync(Func<Task> delete, string serviceName, List<string> failures)
    {
        try
        {
            await delete();
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
