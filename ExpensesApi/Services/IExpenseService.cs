using ExpensesApi.Data.Entities;
using ExpensesApi.Dtos;

namespace ExpensesApi.Services;

public interface IExpenseService
{
    Task<TravelPlanRowEntity?> GetOwnedPlanAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TravelExpenseResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto?> GetAsync(Guid userId, Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto> CreateAsync(Guid userId, Guid travelPlanId, CreateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task<TravelExpenseResponseDto?> UpdateAsync(Guid userId, Guid travelPlanId, Guid expenseId, UpdateTravelExpenseRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid travelPlanId, Guid expenseId, CancellationToken cancellationToken);
    Task<ExpenseSummaryDto?> GetSummaryAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken);
}
