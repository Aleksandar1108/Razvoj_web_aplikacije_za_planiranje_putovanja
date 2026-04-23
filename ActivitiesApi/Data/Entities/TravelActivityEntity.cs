namespace ActivitiesApi.Data.Entities;

public sealed class TravelActivityEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string ActivityTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
