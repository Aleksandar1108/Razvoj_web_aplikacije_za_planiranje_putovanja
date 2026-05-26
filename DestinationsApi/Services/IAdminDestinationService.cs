using DestinationsApi.Dtos;

namespace DestinationsApi.Services;

public interface IAdminDestinationService
{
    Task<IReadOnlyList<AdminDestinationListItemDto>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminTravelPlanOptionDto>> ListTravelPlansAsync(CancellationToken cancellationToken = default);
}
