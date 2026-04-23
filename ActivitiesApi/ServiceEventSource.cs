using System.Diagnostics.Tracing;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ActivitiesApi;

[EventSource(Name = "MyCompany-PlaniranjePutovanja-ActivitiesApi")]
internal sealed class ServiceEventSource : EventSource
{
    public static readonly ServiceEventSource Current = new();

    static ServiceEventSource()
    {
        Task.Run(() => { });
    }

    private ServiceEventSource()
    {
    }

    public static class Keywords
    {
        public const EventKeywords Requests = (EventKeywords)0x1L;
        public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
    }

    [NonEvent]
    public void Message(string message, params object[] args)
    {
        if (IsEnabled())
            Message(string.Format(message, args));
    }

    private const int MessageEventId = 1;

    [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
    public void Message(string message)
    {
        if (IsEnabled())
            WriteEvent(MessageEventId, message);
    }

    [NonEvent]
    public void ServiceMessage(ServiceContext serviceContext, string message, params object[] args)
    {
        if (!IsEnabled())
            return;

        var finalMessage = string.Format(message, args);
        ServiceMessage(
            serviceContext.ServiceName.ToString(),
            serviceContext.ServiceTypeName,
            GetReplicaOrInstanceId(serviceContext),
            serviceContext.PartitionId,
            serviceContext.CodePackageActivationContext.ApplicationName,
            serviceContext.CodePackageActivationContext.ApplicationTypeName,
            serviceContext.NodeContext.NodeName,
            finalMessage);
    }

    private const int ServiceMessageEventId = 2;

    [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
    private void ServiceMessage(
        string serviceName,
        string serviceTypeName,
        long replicaOrInstanceId,
        Guid partitionId,
        string applicationName,
        string applicationTypeName,
        string nodeName,
        string message)
    {
        WriteEvent(ServiceMessageEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, message);
    }

    private const int ServiceTypeRegisteredEventId = 3;

    [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
    public void ServiceTypeRegistered(int hostProcessId, string serviceType)
    {
        WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
    }

    private const int ServiceHostInitializationFailedEventId = 4;

    [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
    public void ServiceHostInitializationFailed(string exception)
    {
        WriteEvent(ServiceHostInitializationFailedEventId, exception);
    }

    private const int ServiceRequestStartEventId = 5;

    [Event(ServiceRequestStartEventId, Level = EventLevel.Informational, Message = "Service request '{0}' started", Keywords = Keywords.Requests)]
    public void ServiceRequestStart(string requestTypeName)
    {
        WriteEvent(ServiceRequestStartEventId, requestTypeName);
    }

    private const int ServiceRequestStopEventId = 6;

    [Event(ServiceRequestStopEventId, Level = EventLevel.Informational, Message = "Service request '{0}' finished", Keywords = Keywords.Requests)]
    public void ServiceRequestStop(string requestTypeName, string exception = "")
    {
        WriteEvent(ServiceRequestStopEventId, requestTypeName, exception);
    }

    private static long GetReplicaOrInstanceId(ServiceContext context)
    {
        if (context is StatelessServiceContext stateless)
            return stateless.InstanceId;
        if (context is StatefulServiceContext stateful)
            return stateful.ReplicaId;
        throw new NotSupportedException("Context type not supported.");
    }
}
