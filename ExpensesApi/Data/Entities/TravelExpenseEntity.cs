namespace ExpensesApi.Data.Entities;

public sealed class TravelExpenseEntity
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
