using System.ComponentModel.DataAnnotations;

namespace TravelPlansApi.Dtos;

public sealed class AdminCreateTravelPlanRequestDto
{
    [Required]
    public Guid OwnerUserId { get; set; }

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

public sealed class AdminTravelPlanListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
