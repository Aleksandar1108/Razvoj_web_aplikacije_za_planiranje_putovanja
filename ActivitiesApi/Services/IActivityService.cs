using ServiceContracts.Dtos;

namespace ActivitiesApi.Services;

public interface IActivityService
{
    Task<IReadOnlyList<TravelActivityResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto?> GetAsync(Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto> CreateAsync(Guid travelPlanId, CreateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto?> UpdateAsync(Guid travelPlanId, Guid activityId, UpdateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);

    Task<int> DeleteAllByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
