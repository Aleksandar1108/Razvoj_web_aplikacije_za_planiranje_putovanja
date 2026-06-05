using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface IActivitiesRemotingService : IService
{
    Task<List<TravelActivityResponseDto>> ListActivitiesAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto> GetActivityAsync(ServiceCallContext context, Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto> CreateActivityAsync(ServiceCallContext context, Guid travelPlanId, CreateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task<TravelActivityResponseDto> UpdateActivityAsync(ServiceCallContext context, Guid travelPlanId, Guid activityId, UpdateTravelActivityRequestDto request, CancellationToken cancellationToken);
    Task DeleteActivityAsync(ServiceCallContext context, Guid travelPlanId, Guid activityId, CancellationToken cancellationToken);

    Task<ActivityCostSumDto> GetEstimatedCostSumAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
