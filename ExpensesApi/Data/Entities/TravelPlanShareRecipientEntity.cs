namespace ExpensesApi.Data.Entities;

public sealed class TravelPlanShareRecipientEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public DateTime ClaimedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
