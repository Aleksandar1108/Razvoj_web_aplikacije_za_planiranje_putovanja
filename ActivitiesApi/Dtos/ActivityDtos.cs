using System.ComponentModel.DataAnnotations;

namespace ActivitiesApi.Dtos;

public static class ActivityStatuses
{
    public const string Planned = "planned";
    public const string Reserved = "reserved";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] Allowed = [Planned, Reserved, Completed, Cancelled];
}

public sealed class TravelActivityResponseDto
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
    public string ActivityTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Status { get; set; } = ActivityStatuses.Planned;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class CreateTravelActivityRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? ActivityDate { get; set; }

    [Required]
    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    public string ActivityTime { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public decimal EstimatedCost { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = ActivityStatuses.Planned;
}

public sealed class UpdateTravelActivityRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? ActivityDate { get; set; }

    [Required]
    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    public string ActivityTime { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public decimal EstimatedCost { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = ActivityStatuses.Planned;
}
