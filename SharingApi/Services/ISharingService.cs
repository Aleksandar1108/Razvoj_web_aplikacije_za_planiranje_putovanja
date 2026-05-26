using SharingApi.Dtos;

namespace SharingApi.Services;

public interface ISharingService
{
    Task<CreateTravelPlanShareLinkResponseDto> CreateShareLinkAsync(
        Guid ownerUserId,
        Guid travelPlanId,
        CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken);

    Task<ClaimShareLinkResponseDto?> ClaimShareLinkAsync(
        Guid recipientUserId,
        ClaimShareLinkRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedTravelPlanListItemDto>> ListSharedPlansForUserAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken);

    Task<int> DeleteAllByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
