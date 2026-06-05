using CrossService;
using ServiceContracts.Remoting;

namespace ApiGateway.Infrastructure;

public sealed class RemotingServices
{
    public IWeb1RemotingService Web1 { get; } =
        ServiceFabricRemoting.CreateProxy<IWeb1RemotingService>(ServiceFabricRemoting.ServiceNames.Web1);

    public ITravelPlansRemotingService TravelPlans { get; } =
        ServiceFabricRemoting.CreateProxy<ITravelPlansRemotingService>(ServiceFabricRemoting.ServiceNames.TravelPlansApi);

    public IDestinationsRemotingService Destinations { get; } =
        ServiceFabricRemoting.CreateProxy<IDestinationsRemotingService>(ServiceFabricRemoting.ServiceNames.DestinationsApi);

    public IActivitiesRemotingService Activities { get; } =
        ServiceFabricRemoting.CreateProxy<IActivitiesRemotingService>(ServiceFabricRemoting.ServiceNames.ActivitiesApi);

    public IExpensesRemotingService Expenses { get; } =
        ServiceFabricRemoting.CreateProxy<IExpensesRemotingService>(ServiceFabricRemoting.ServiceNames.ExpensesApi);

    public IChecklistRemotingService Checklist { get; } =
        ServiceFabricRemoting.CreateProxy<IChecklistRemotingService>(ServiceFabricRemoting.ServiceNames.ChecklistApi);

    public ISharingRemotingService Sharing { get; } =
        ServiceFabricRemoting.CreateProxy<ISharingRemotingService>(ServiceFabricRemoting.ServiceNames.SharingApi);
}
