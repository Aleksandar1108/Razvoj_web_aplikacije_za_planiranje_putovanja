using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
using ServiceContracts.Dtos;
using TravelPlansApi.Data;
using TravelPlansApi.Data.Entities;

namespace TravelPlansApi.Services;

public sealed class TravelPlanService : ITravelPlanService
{
    private readonly TravelPlansDbContext _db;
    private readonly IPlanCascadeDeleteClient _cascadeDelete;

    public TravelPlanService(TravelPlansDbContext db, IPlanCascadeDeleteClient cascadeDelete)
    {
        _db = db;
        _cascadeDelete = cascadeDelete;
    }

    public async Task<IReadOnlyList<TravelPlanResponseDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await _db.TravelPlans.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<TravelPlanResponseDto?> GetForUserAsync(Guid userId, Guid planId, CancellationToken cancellationToken)
    {
        var row = await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TravelPlanResponseDto?> GetByIdAsync(Guid planId, CancellationToken cancellationToken)
    {
        var row = await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TravelPlanResponseDto> CreateAsync(Guid userId, CreateTravelPlanRequestDto request, CancellationToken cancellationToken)
    {
        var start = DateContract.RequireDateOnly(request.StartDate);
        var end = DateContract.RequireDateOnly(request.EndDate);
        if (end < start)
            throw new ArgumentException("Krajnji datum ne može biti prije početnog.");
        if (request.PlannedBudget < 0)
            throw new ArgumentException("Planirani budžet ne može biti negativan.");

        var now = DateTime.UtcNow;
        var entity = new TravelPlanEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription.Trim(),
            StartDate = start,
            EndDate = end,
            PlannedBudget = request.PlannedBudget,
            GeneralNotes = string.IsNullOrWhiteSpace(request.GeneralNotes) ? null : request.GeneralNotes.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.TravelPlans.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TravelPlanResponseDto?> UpdateAsync(Guid userId, Guid planId, UpdateTravelPlanRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await _db.TravelPlans.FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId, cancellationToken);
        if (entity is null)
            return null;

        ApplyUpdate(entity, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TravelPlanResponseDto?> UpdateByPlanIdAsync(
        Guid planId,
        UpdateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.TravelPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (entity is null)
            return null;

        ApplyUpdate(entity, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid planId, CancellationToken cancellationToken)
    {
        var entity = await _db.TravelPlans.FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId, cancellationToken);
        if (entity is null)
            return false;

        await _cascadeDelete.DeleteAllRelatedDataAsync(planId, cancellationToken);

        _db.TravelPlans.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteByPlanIdAsync(Guid planId, CancellationToken cancellationToken)
    {
        var entity = await _db.TravelPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (entity is null)
            return false;

        await _cascadeDelete.DeleteAllRelatedDataAsync(planId, cancellationToken);

        _db.TravelPlans.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyUpdate(TravelPlanEntity entity, UpdateTravelPlanRequestDto request)
    {
        var start = DateContract.RequireDateOnly(request.StartDate);
        var end = DateContract.RequireDateOnly(request.EndDate);
        if (end < start)
            throw new ArgumentException("Krajnji datum ne može biti prije početnog.");
        if (request.PlannedBudget < 0)
            throw new ArgumentException("Planirani budžet ne može biti negativan.");

        entity.Name = request.Name.Trim();
        entity.ShortDescription = request.ShortDescription.Trim();
        entity.StartDate = start;
        entity.EndDate = end;
        entity.PlannedBudget = request.PlannedBudget;
        entity.GeneralNotes = string.IsNullOrWhiteSpace(request.GeneralNotes) ? null : request.GeneralNotes.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static TravelPlanResponseDto Map(TravelPlanEntity e) =>
        new()
        {
            Id = e.Id,
            Name = e.Name,
            ShortDescription = e.ShortDescription,
            StartDate = DateContract.FromDateOnly(e.StartDate),
            EndDate = DateContract.FromDateOnly(e.EndDate),
            PlannedBudget = e.PlannedBudget,
            GeneralNotes = e.GeneralNotes,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
}
