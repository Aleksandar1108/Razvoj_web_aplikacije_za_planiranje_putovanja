using ExpensesApi.Data.Entities;
using ExpensesApi.Dtos;

namespace ExpensesApi.Services;

public interface IExpenseService
{
    Task<TravelPlanRowEntity?> GetPlanAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelExpenseResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto?> GetAsync(Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto> CreateAsync(Guid travelPlanId, CreateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto?> UpdateAsync(Guid travelPlanId, Guid expenseId, UpdateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);
    Task<ExpenseSummaryDto?> GetSummaryAsync(Guid travelPlanId, CancellationToken cancellationToken);
}
