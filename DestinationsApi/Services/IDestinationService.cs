using ServiceContracts.Dtos;

namespace DestinationsApi.Services;

public interface IDestinationService
{
    Task<IReadOnlyList<TravelDestinationResponseDto>> ListByTravelPlanIdAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto?> GetAsync(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto> CreateAsync(
        Guid travelPlanId,
        CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken);

    Task<TravelDestinationResponseDto?> UpdateAsync(
        Guid travelPlanId,
        Guid destinationId,
        UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken);

    Task<int> DeleteAllByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
