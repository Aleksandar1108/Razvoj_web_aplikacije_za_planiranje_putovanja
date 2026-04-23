namespace ExpensesApi.Data.Entities;

public sealed class TravelPlanRowEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal PlannedBudget { get; set; }
}
