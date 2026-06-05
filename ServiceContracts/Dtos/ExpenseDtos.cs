using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.Dtos;

public static class ExpenseCategories
{
    public const string Transport = "transport";
    public const string Accommodation = "accommodation";
    public const string Food = "food";
    public const string Tickets = "tickets";
    public const string Shopping = "shopping";
    public const string Other = "other";

    public static readonly string[] Allowed = [Transport, Accommodation, Food, Tickets, Shopping, Other];
}

public sealed class TravelExpenseResponseDto
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = ExpenseCategories.Other;
    public decimal Amount { get; set; }
    public string ExpenseDate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ExpenseSummaryDto
{
    public decimal PlannedBudget { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalExpenseEntries { get; set; }
    public decimal TotalActivityEstimatedCosts { get; set; }
    public decimal RemainingBudget { get; set; }
}

public sealed class CreateTravelExpenseRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = ExpenseCategories.Other;

    public decimal Amount { get; set; }

    [Required]
    public string? ExpenseDate { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public sealed class UpdateTravelExpenseRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = ExpenseCategories.Other;

    public decimal Amount { get; set; }

    [Required]
    public string? ExpenseDate { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }
}
