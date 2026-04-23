namespace ExpensesApi.Data.Entities;

public sealed class TravelActivityEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public decimal EstimatedCost { get; set; }
}
