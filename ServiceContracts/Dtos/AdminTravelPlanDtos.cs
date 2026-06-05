using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.Dtos;

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
    public string? StartDate { get; set; }

    [Required]
    public string? EndDate { get; set; }

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
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}
