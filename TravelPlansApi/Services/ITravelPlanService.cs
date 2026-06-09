using ServiceContracts.Dtos;

namespace TravelPlansApi.Services;

public interface ITravelPlanService
{
    Task<IReadOnlyList<TravelPlanResponseDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<TravelPlanResponseDto?> GetForUserAsync(Guid userId, Guid planId, CancellationToken cancellationToken);

    Task<TravelPlanResponseDto?> GetByIdAsync(Guid planId, CancellationToken cancellationToken);

    Task<TravelPlanResponseDto> CreateAsync(Guid userId, CreateTravelPlanRequestDto request, CancellationToken cancellationToken);

    Task<TravelPlanResponseDto?> UpdateAsync(Guid userId, Guid planId, UpdateTravelPlanRequestDto request, CancellationToken cancellationToken);

    Task<TravelPlanResponseDto?> UpdateByPlanIdAsync(Guid planId, UpdateTravelPlanRequestDto request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid planId, CancellationToken cancellationToken);

    Task<bool> DeleteByPlanIdAsync(Guid planId, CancellationToken cancellationToken);

    Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
