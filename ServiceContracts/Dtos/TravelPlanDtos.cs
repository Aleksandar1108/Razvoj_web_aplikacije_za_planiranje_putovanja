using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ServiceContracts.Dtos;

[DataContract]
public sealed class TravelPlanResponseDto
{
    [DataMember]
    public Guid Id { get; init; }

    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember]
    public string ShortDescription { get; init; } = string.Empty;

    [DataMember]
    public string StartDate { get; init; } = string.Empty;

    [DataMember]
    public string EndDate { get; init; } = string.Empty;

    [DataMember]
    public decimal PlannedBudget { get; init; }

    [DataMember]
    public string? GeneralNotes { get; init; }

    [DataMember]
    public DateTime CreatedAtUtc { get; init; }

    [DataMember]
    public DateTime UpdatedAtUtc { get; init; }
}

[DataContract]
public sealed class CreateTravelPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [DataMember]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    [DataMember]
    public string? StartDate { get; set; }

    [Required]
    [DataMember]
    public string? EndDate { get; set; }

    [DataMember]
    public decimal PlannedBudget { get; set; }

    [MaxLength(4000)]
    [DataMember]
    public string? GeneralNotes { get; set; }
}

[DataContract]
public sealed class UpdateTravelPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [DataMember]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    [DataMember]
    public string? StartDate { get; set; }

    [Required]
    [DataMember]
    public string? EndDate { get; set; }

    [DataMember]
    public decimal PlannedBudget { get; set; }

    [MaxLength(4000)]
    [DataMember]
    public string? GeneralNotes { get; set; }
}
