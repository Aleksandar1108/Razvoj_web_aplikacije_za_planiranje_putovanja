using DestinationsApi.Data.Entities;
using DestinationsApi.Dtos;

namespace DestinationsApi.Services;

public interface IDestinationService
{
    Task<TravelPlanRowEntity?> GetOwnedPlanAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TravelDestinationResponseDto>> ListByTravelPlanIdAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto?> GetAsync(
        Guid userId,
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto> CreateAsync(
        Guid userId,
        Guid travelPlanId,
        CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto?> UpdateAsync(
        Guid userId,
        Guid travelPlanId,
        Guid destinationId,
        UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken);
}
