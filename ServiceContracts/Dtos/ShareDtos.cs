using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.Dtos;

public sealed class CreateTravelPlanShareLinkRequestDto
{
    [Required]
    [RegularExpression("^(view|edit)$", ErrorMessage = "Permission mora biti 'view' ili 'edit'.")]
    public string Permission { get; set; } = string.Empty;
}

public sealed class CreateTravelPlanShareLinkResponseDto
{
    public Guid TravelPlanId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string QrPayloadJson { get; set; } = string.Empty;
}

public sealed class ClaimShareLinkRequestDto
{
    [Required]
    [MinLength(10)]
    public string Token { get; set; } = string.Empty;
}

public sealed class ClaimShareLinkResponseDto
{
    public Guid TravelPlanId { get; set; }
    public string Permission { get; set; } = string.Empty;
}

public sealed class SharedTravelPlanListItemDto
{
    public Guid TravelPlanId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
