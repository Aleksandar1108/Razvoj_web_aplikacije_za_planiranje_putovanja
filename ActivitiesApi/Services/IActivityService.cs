using ActivitiesApi.Data.Entities;
using ActivitiesApi.Dtos;

namespace ActivitiesApi.Services;

public interface IActivityService
{
    Task<TravelPlanRowEntity?> GetOwnedPlanAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelActivityResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto?> GetAsync(Guid userId, Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto> CreateAsync(Guid userId, Guid travelPlanId, CreateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto?> UpdateAsync(Guid userId, Guid travelPlanId, Guid activityId, UpdateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);
}
