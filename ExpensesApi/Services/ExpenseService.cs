using CrossService.Clients;
using ExpensesApi.Data;
using ExpensesApi.Data.Entities;
using ExpensesApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExpensesApi.Services;

public sealed class ExpenseService : IExpenseService
{
    private readonly ExpensesDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;
    private readonly IActivitiesInternalClient _activities;

    public ExpenseService(
        ExpensesDbContext db,
        ITravelPlansInternalClient travelPlans,
        IActivitiesInternalClient activities)
    {
        _db = db;
        _travelPlans = travelPlans;
        _activities = activities;
    }

    public async Task<IReadOnlyList<TravelExpenseResponseDto>> ListByTravelPlanIdAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.TravelExpenses.AsNoTracking()
            .Where(e => e.TravelPlanId == travelPlanId)
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<TravelExpenseResponseDto?> GetAsync(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var row = await _db.TravelExpenses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == expenseId && d.TravelPlanId == travelPlanId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TravelExpenseResponseDto> CreateAsync(
        Guid travelPlanId,
        CreateTravelExpenseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            throw new InvalidOperationException("Plan putovanja nije pronađen.");

        var category = NormalizeCategory(request.Category);
        ValidateAmount(request.Amount);
        var now = DateTime.UtcNow;

        var entity = new TravelExpenseEntity
        {
            Id = Guid.NewGuid(),
            TravelPlanId = travelPlanId,
            Name = request.Name.Trim(),
            Category = category,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate!.Value,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.TravelExpenses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TravelExpenseResponseDto?> UpdateAsync(
        Guid travelPlanId,
        Guid expenseId,
        UpdateTravelExpenseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var entity = await _db.TravelExpenses
            .FirstOrDefaultAsync(d => d.Id == expenseId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return null;

        var category = NormalizeCategory(request.Category);
        ValidateAmount(request.Amount);

        entity.Name = request.Name.Trim();
        entity.Category = category;
        entity.Amount = request.Amount;
        entity.ExpenseDate = request.ExpenseDate!.Value;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(
        Guid travelPlanId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return false;

        var entity = await _db.TravelExpenses
            .FirstOrDefaultAsync(d => d.Id == expenseId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return false;

        _db.TravelExpenses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ExpenseSummaryDto?> GetSummaryAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var plan = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken);
        if (plan is null)
            return null;

        var total = await _db.TravelExpenses.AsNoTracking()
            .Where(e => e.TravelPlanId == travelPlanId)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var totalActivityEstimatedCosts = await _activities.GetEstimatedCostSumAsync(travelPlanId, cancellationToken);
        var totalCombined = total + totalActivityEstimatedCosts;

        return new ExpenseSummaryDto
        {
            PlannedBudget = plan.PlannedBudget,
            TotalExpenses = totalCombined,
            TotalExpenseEntries = total,
            TotalActivityEstimatedCosts = totalActivityEstimatedCosts,
            RemainingBudget = plan.PlannedBudget - totalCombined
        };
    }

    private Task<bool> PlanExistsAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        _travelPlans.ExistsAsync(travelPlanId, cancellationToken);

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Iznos troška ne može biti negativan.");
    }

    private static string NormalizeCategory(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        if (!ExpenseCategories.Allowed.Contains(normalized))
            throw new ArgumentException("Kategorija mora biti: transport, accommodation, food, tickets, shopping ili other.");
        return normalized;
    }

    private static TravelExpenseResponseDto Map(TravelExpenseEntity e) =>
        new()
        {
            Id = e.Id,
            TravelPlanId = e.TravelPlanId,
            Name = e.Name,
            Category = e.Category,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            Description = e.Description,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
}
