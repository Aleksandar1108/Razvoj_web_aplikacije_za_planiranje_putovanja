using System.Diagnostics.Tracing;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ApiGateway;

[EventSource(Name = "MyCompany-PlaniranjePutovanja-ApiGateway")]
internal sealed class ServiceEventSource : EventSource
{
    public static readonly ServiceEventSource Current = new();

    private ServiceEventSource() { }

    [NonEvent]
    public void Message(string message, params object[] args)
    {
        if (IsEnabled())
            Message(string.Format(message, args));
    }

    [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
    private void Message(string message)
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
            GetReplicaOrInstanceId(serviceContext),
            serviceContext.PartitionId,
            serviceContext.NodeContext.NodeName,
            finalMessage);
    }

    [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{4}")]
    private void ServiceMessage(
        string serviceName,
        long replicaOrInstanceId,
        Guid partitionId,
        string nodeName,
        string message)
    {
        if (IsEnabled())
            WriteEvent(ServiceMessageEventId, serviceName, replicaOrInstanceId, partitionId, nodeName, message);
    }

    [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
    public void ServiceTypeRegistered(int hostProcessId, string serviceType)
    {
        if (IsEnabled())
            WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
    }

    [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
    public void ServiceHostInitializationFailed(string exception)
    {
        if (IsEnabled())
            WriteEvent(ServiceHostInitializationFailedEventId, exception);
    }

    private static long GetReplicaOrInstanceId(ServiceContext context) =>
        context switch
        {
            StatelessServiceContext stateless => stateless.InstanceId,
            StatefulServiceContext stateful => stateful.ReplicaId,
            _ => 0
        };

    private const int MessageEventId = 1;
    private const int ServiceMessageEventId = 2;
    private const int ServiceTypeRegisteredEventId = 3;
    private const int ServiceHostInitializationFailedEventId = 4;

    public static class Keywords
    {
        public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
    }
}
