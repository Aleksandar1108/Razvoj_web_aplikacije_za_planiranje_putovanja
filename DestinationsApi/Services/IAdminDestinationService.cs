using ServiceContracts;
using ServiceContracts.Dtos;

namespace DestinationsApi.Services;

public interface IAdminDestinationService
{
    Task<IReadOnlyList<AdminDestinationListItemDto>> ListAllAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminTravelPlanOptionDto>> ListTravelPlansAsync(
        ServiceCallContext context,
        CancellationToken cancellationToken = default);
}
