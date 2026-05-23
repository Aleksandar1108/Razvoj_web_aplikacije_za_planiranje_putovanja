namespace DestinationsApi.Data.Entities;

public sealed class TravelPlanShareLinkEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public string Permission { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
