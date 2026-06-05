using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface ISharingRemotingService : IService
{
    Task<CreateTravelPlanShareLinkResponseDto> CreateShareLinkAsync(ServiceCallContext context, Guid travelPlanId, CreateTravelPlanShareLinkRequestDto request, CancellationToken cancellationToken);
    Task<List<SharedTravelPlanListItemDto>> ListSharedPlansAsync(ServiceCallContext context, CancellationToken cancellationToken);
    Task<ClaimShareLinkResponseDto> ClaimShareLinkAsync(ServiceCallContext context, ClaimShareLinkRequestDto request, CancellationToken cancellationToken);

    Task<ShareAccessDto> ResolveShareTokenAccessAsync(ServiceCallContext context, Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);
    Task<ShareAccessDto> ResolveRecipientAccessAsync(ServiceCallContext context, Guid travelPlanId, bool requiresMutation, CancellationToken cancellationToken);

    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
