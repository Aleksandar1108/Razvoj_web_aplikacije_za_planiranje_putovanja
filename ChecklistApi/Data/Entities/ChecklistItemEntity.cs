namespace ChecklistApi.Data.Entities;

public sealed class ChecklistItemEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
