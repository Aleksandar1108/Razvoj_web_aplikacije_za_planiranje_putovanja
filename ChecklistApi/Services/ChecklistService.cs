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

    public async Task<TravelPlanRowEntity?> GetOwnedPlanAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken)
    {
        return await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == travelPlanId && p.UserId == userId, cancellationToken);
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

    public async Task<ChecklistItemResponseDto?> GetAsync(Guid userId, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await PlanOwnedAsync(userId, travelPlanId, cancellationToken))
            return null;

        var row = await _db.ChecklistItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<ChecklistItemResponseDto> CreateAsync(Guid userId, Guid travelPlanId, CreateChecklistItemRequestDto request, CancellationToken cancellationToken)
    {
        if (!await PlanOwnedAsync(userId, travelPlanId, cancellationToken))
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

    public async Task<ChecklistItemResponseDto?> UpdateAsync(Guid userId, Guid travelPlanId, Guid itemId, UpdateChecklistItemRequestDto request, CancellationToken cancellationToken)
    {
        if (!await PlanOwnedAsync(userId, travelPlanId, cancellationToken))
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

    public async Task<ChecklistItemResponseDto?> ToggleAsync(Guid userId, Guid travelPlanId, Guid itemId, bool isDone, CancellationToken cancellationToken)
    {
        if (!await PlanOwnedAsync(userId, travelPlanId, cancellationToken))
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

    public async Task<bool> DeleteAsync(Guid userId, Guid travelPlanId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await PlanOwnedAsync(userId, travelPlanId, cancellationToken))
            return false;

        var entity = await _db.ChecklistItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return false;

        _db.ChecklistItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> PlanOwnedAsync(Guid userId, Guid travelPlanId, CancellationToken cancellationToken)
    {
        return await _db.TravelPlans.AsNoTracking()
            .AnyAsync(p => p.Id == travelPlanId && p.UserId == userId, cancellationToken);
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
