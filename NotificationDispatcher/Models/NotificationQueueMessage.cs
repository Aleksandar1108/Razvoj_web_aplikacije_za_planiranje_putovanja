using System.Runtime.Serialization;

namespace NotificationDispatcher.Models;

[DataContract]
public sealed class NotificationQueueMessage
{
    [DataMember]
    public Guid OwnerUserId { get; set; }

    [DataMember]
    public Guid TravelPlanId { get; set; }

    [DataMember]
    public string Category { get; set; } = string.Empty;

    [DataMember]
    public string Action { get; set; } = string.Empty;

    [DataMember]
    public string? ItemLabel { get; set; }

    [DataMember]
    public Guid? RelatedEntityId { get; set; }

    [DataMember]
    public bool? ChecklistDone { get; set; }
}
