using System.ComponentModel.DataAnnotations;

namespace ChecklistApi.Dtos;

public sealed class ChecklistItemResponseDto
{
    public Guid Id { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class CreateChecklistItemRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
}

public sealed class UpdateChecklistItemRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }
}

public sealed class ToggleChecklistItemRequestDto
{
    public bool IsDone { get; set; }
}
