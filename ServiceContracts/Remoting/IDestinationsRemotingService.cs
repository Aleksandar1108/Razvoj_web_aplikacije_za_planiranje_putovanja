using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface IDestinationsRemotingService : IService
{
    Task<List<TravelDestinationResponseDto>> ListDestinationsAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelDestinationResponseDto> GetDestinationAsync(ServiceCallContext context, Guid travelPlanId, Guid destinationId, CancellationToken cancellationToken);
    Task<TravelDestinationResponseDto> CreateDestinationAsync(ServiceCallContext context, Guid travelPlanId, CreateTravelDestinationRequestDto request, CancellationToken cancellationToken);
    Task<TravelDestinationResponseDto> UpdateDestinationAsync(ServiceCallContext context, Guid travelPlanId, Guid destinationId, UpdateTravelDestinationRequestDto request, CancellationToken cancellationToken);
    Task DeleteDestinationAsync(ServiceCallContext context, Guid travelPlanId, Guid destinationId, CancellationToken cancellationToken);

    Task<List<AdminDestinationListItemDto>> ListAdminDestinationsAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<List<AdminTravelPlanOptionDto>> ListAdminTravelPlanOptionsAsync(ServiceCallContext context, CancellationToken cancellationToken);

    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
