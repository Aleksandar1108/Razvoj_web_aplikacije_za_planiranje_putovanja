using ChecklistApi.Data.Entities;
using ChecklistApi.Dtos;

namespace ChecklistApi.Services;

public interface IChecklistService
{
    Task<TravelPlanRowEntity?> GetOwnedPlanAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChecklistItemResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> GetAsync(Guid userId, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> CreateAsync(Guid userId, Guid travelPlanId, CreateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> UpdateAsync(Guid userId, Guid travelPlanId, Guid itemId, UpdateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto?> ToggleAsync(Guid userId, Guid travelPlanId, Guid itemId, bool isDone, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);
}
