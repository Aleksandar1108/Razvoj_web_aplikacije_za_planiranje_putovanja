using Microsoft.ServiceFabric.Services.Remoting;
using ServiceContracts.Dtos;

namespace ServiceContracts.Remoting;

public interface IExpensesRemotingService : IService
{
    Task<List<TravelExpenseResponseDto>> ListExpensesAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<ExpenseSummaryDto> GetExpenseSummaryAsync(ServiceCallContext context, Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto> GetExpenseAsync(ServiceCallContext context, Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto> CreateExpenseAsync(ServiceCallContext context, Guid travelPlanId, CreateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto> UpdateExpenseAsync(ServiceCallContext context, Guid travelPlanId, Guid expenseId, UpdateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task DeleteExpenseAsync(ServiceCallContext context, Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);

    Task<CascadeDeleteResultDto> CascadeDeleteTravelPlanDataAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
