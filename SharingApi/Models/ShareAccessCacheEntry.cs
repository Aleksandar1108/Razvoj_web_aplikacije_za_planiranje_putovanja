using System.Runtime.Serialization;

namespace SharingApi.Models;

[DataContract]
public sealed class ShareAccessCacheEntry
{
    [DataMember]
    public string Kind { get; set; } = "none";

    [DataMember]
    public DateTime CachedAtUtc { get; set; }
}
