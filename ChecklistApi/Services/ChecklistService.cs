using ChecklistApi.Data;
using ChecklistApi.Data.Entities;
using ChecklistApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ChecklistApi.Services;

public sealed class ChecklistService : IChecklistService
{
    private readonly ChecklistDbContext _db;

    public ChecklistService(ChecklistDbContext db)
    {
        _db = db;
    }

    public async Task<TravelPlanRowEntity?> GetPlanAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        return await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == travelPlanId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemResponseDto>> ListByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var rows = await _db.ChecklistItems.AsNoTracking()
            .Where(x => x.TravelPlanId == travelPlanId)
            .OrderBy(x => x.IsDone)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ChecklistItemResponseDto?> GetAsync(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var row = await _db.ChecklistItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<ChecklistItemResponseDto> CreateAsync(Guid travelPlanId, CreateChecklistItemRequestDto request, CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            throw new InvalidOperationException("Plan putovanja nije pronađen.");

        var now = DateTime.UtcNow;
        var entity = new ChecklistItemEntity
        {
            Id = Guid.NewGuid(),
            TravelPlanId = travelPlanId,
            Title = request.Title.Trim(),
            IsDone = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.ChecklistItems.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ChecklistItemResponseDto?> UpdateAsync(Guid travelPlanId, Guid itemId, UpdateChecklistItemRequestDto request, CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var entity = await _db.ChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return null;

        entity.Title = request.Title.Trim();
        entity.IsDone = request.IsDone;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ChecklistItemResponseDto?> ToggleAsync(Guid travelPlanId, Guid itemId, bool isDone, CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var entity = await _db.ChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return null;

        entity.IsDone = isDone;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return false;

        var entity = await _db.ChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return false;

        _db.ChecklistItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> PlanExistsAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        return await _db.TravelPlans.AsNoTracking()
            .AnyAsync(p => p.Id == travelPlanId, cancellationToken);
    }

    private static ChecklistItemResponseDto Map(ChecklistItemEntity e) =>
        new()
        {
            Id = e.Id,
            TravelPlanId = e.TravelPlanId,
            Title = e.Title,
            IsDone = e.IsDone,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
}
