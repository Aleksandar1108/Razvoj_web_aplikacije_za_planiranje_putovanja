using System.ComponentModel.DataAnnotations;

namespace DestinationsApi.Dtos;

public sealed class TravelDestinationResponseDto
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class CreateTravelDestinationRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateOnly? ArrivalDate { get; set; }

    [Required]
    public DateOnly? DepartureDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public sealed class UpdateTravelDestinationRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateOnly? ArrivalDate { get; set; }

    [Required]
    public DateOnly? DepartureDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
