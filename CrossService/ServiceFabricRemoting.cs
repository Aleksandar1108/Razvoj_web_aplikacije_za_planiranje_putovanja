using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace CrossService;

public static class ServiceFabricRemoting
{
    public const string ApplicationName = "PlaniranjePutovanja";

    public static class ServiceNames
    {
        public const string Web1 = "Web1";
        public const string TravelPlansApi = "TravelPlansApi";
        public const string DestinationsApi = "DestinationsApi";
        public const string ActivitiesApi = "ActivitiesApi";
        public const string ExpensesApi = "ExpensesApi";
        public const string ChecklistApi = "ChecklistApi";
        public const string SharingApi = "SharingApi";
        public const string NotificationDispatcher = "NotificationDispatcher";
    }

    private static readonly HashSet<string> StatefulServices = new(StringComparer.OrdinalIgnoreCase)
    {
        ServiceNames.SharingApi,
        ServiceNames.NotificationDispatcher
    };

    private static readonly ServiceProxyFactory ProxyFactory = new(_ => new FabricTransportServiceRemotingClientFactory());

    public static string GetServiceUri(string serviceName) =>
        $"fabric:/{ApplicationName}/{serviceName}";

    public static T CreateProxy<T>(string serviceName)
        where T : IService
    {
        var uri = new Uri(GetServiceUri(serviceName));
        if (StatefulServices.Contains(serviceName))
            return ProxyFactory.CreateServiceProxy<T>(uri, new ServicePartitionKey(0L));
        return ProxyFactory.CreateServiceProxy<T>(uri);
    }
}
