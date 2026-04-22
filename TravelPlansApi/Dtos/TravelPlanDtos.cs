using System.ComponentModel.DataAnnotations;

namespace TravelPlansApi.Dtos;

public sealed class TravelPlanResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal PlannedBudget { get; init; }
    public string? GeneralNotes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class CreateTravelPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    public decimal PlannedBudget { get; set; }

    [MaxLength(4000)]
    public string? GeneralNotes { get; set; }
}

public sealed class UpdateTravelPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    public decimal PlannedBudget { get; set; }

    [MaxLength(4000)]
    public string? GeneralNotes { get; set; }
}
