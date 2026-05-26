using ChecklistApi.Dtos;

namespace ChecklistApi.Services;

public interface IChecklistService
{
    Task<IReadOnlyList<ChecklistItemResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> GetAsync(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> CreateAsync(Guid travelPlanId, CreateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> UpdateAsync(Guid travelPlanId, Guid itemId, UpdateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> ToggleAsync(Guid travelPlanId, Guid itemId, bool isDone, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);
}
