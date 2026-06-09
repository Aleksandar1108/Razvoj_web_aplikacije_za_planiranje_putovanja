using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface ITravelPlansRemotingService : IService
{
    Task<List<TravelPlanResponseDto>> ListTravelPlansAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<TravelPlanResponseDto> GetTravelPlanAsync(ServiceCallContext context, Guid id, CancellationToken cancellationToken);
    Task<TravelPlanResponseDto> CreateTravelPlanAsync(ServiceCallContext context, CreateTravelPlanRequestDto request, CancellationToken cancellationToken);
    Task<TravelPlanResponseDto> UpdateTravelPlanAsync(ServiceCallContext context, Guid id, UpdateTravelPlanRequestDto request, CancellationToken cancellationToken);
    Task DeleteTravelPlanAsync(ServiceCallContext context, Guid id, CancellationToken cancellationToken);

    Task<List<AdminTravelPlanListItemDto>> ListAdminTravelPlansAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<TravelPlanResponseDto> AdminCreateTravelPlanAsync(ServiceCallContext context, AdminCreateTravelPlanRequestDto request, CancellationToken cancellationToken);

    Task<TravelPlanMetaDto> GetMetaAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelPlanExistsDto> ExistsAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelPlanOwnerDto> GetOwnerAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<List<TravelPlanMetaDto>> GetMetaBatchAsync(ServiceCallContext context, SharedPlanMetaBatchRequestDto request, CancellationToken cancellationToken);

    Task DeleteAllPlansForUserAsync(Guid userId, CancellationToken cancellationToken);
}
