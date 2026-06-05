using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface IChecklistRemotingService : IService
{
    Task<List<ChecklistItemResponseDto>> ListChecklistItemsAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> CreateChecklistItemAsync(ServiceCallContext context, Guid travelPlanId, CreateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> GetChecklistItemAsync(ServiceCallContext context, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> UpdateChecklistItemAsync(ServiceCallContext context, Guid travelPlanId, Guid itemId, UpdateChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task<ChecklistItemResponseDto> ToggleChecklistItemAsync(ServiceCallContext context, Guid travelPlanId, Guid itemId, ToggleChecklistItemRequestDto request, CancellationToken cancellationToken);
    Task DeleteChecklistItemAsync(ServiceCallContext context, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken);

    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
