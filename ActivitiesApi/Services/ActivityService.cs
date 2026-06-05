using ActivitiesApi.Data;
using ActivitiesApi.Data.Entities;
using CrossService.Clients;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
using ServiceContracts.Dtos;

namespace ActivitiesApi.Services;

public sealed class ActivityService : IActivityService
{
    private readonly ActivitiesDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;

    public ActivityService(ActivitiesDbContext db, ITravelPlansInternalClient travelPlans)
    {
        _db = db;
        _travelPlans = travelPlans;
    }

    public async Task<IReadOnlyList<TravelActivityResponseDto>> ListByTravelPlanIdAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.TravelActivities.AsNoTracking()
            .Where(a => a.TravelPlanId == travelPlanId)
            .OrderBy(a => a.ActivityDate)
            .ThenBy(a => a.ActivityTime)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<TravelActivityResponseDto?> GetAsync(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return null;

        var row = await _db.TravelActivities.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == activityId && d.TravelPlanId == travelPlanId,
                cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TravelActivityResponseDto> CreateAsync(
        Guid travelPlanId,
        CreateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        var plan = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken)
                   ?? throw new InvalidOperationException("Plan putovanja nije pronađen.");

        var date = DateContract.RequireDateOnly(request.ActivityDate);
        ValidateAgainstPlan(date, plan);
        var status = NormalizeStatus(request.Status);
        ValidateEstimatedCost(request.EstimatedCost);

        var now = DateTime.UtcNow;
        var entity = new TravelActivityEntity
        {
            Id = Guid.NewGuid(),
            TravelPlanId = travelPlanId,
            Name = request.Name.Trim(),
            ActivityDate = date,
            ActivityTime = request.ActivityTime.Trim(),
            Location = request.Location.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EstimatedCost = request.EstimatedCost,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.TravelActivities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TravelActivityResponseDto?> UpdateAsync(
        Guid travelPlanId,
        Guid activityId,
        UpdateTravelActivityRequestDto request,
        CancellationToken cancellationToken)
    {
        var plan = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken);
        if (plan is null)
            return null;

        var entity = await _db.TravelActivities
            .FirstOrDefaultAsync(d => d.Id == activityId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return null;

        var date = DateContract.RequireDateOnly(request.ActivityDate);
        ValidateAgainstPlan(date, plan);
        var status = NormalizeStatus(request.Status);
        ValidateEstimatedCost(request.EstimatedCost);

        entity.Name = request.Name.Trim();
        entity.ActivityDate = date;
        entity.ActivityTime = request.ActivityTime.Trim();
        entity.Location = request.Location.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.EstimatedCost = request.EstimatedCost;
        entity.Status = status;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(
        Guid travelPlanId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return false;

        var entity = await _db.TravelActivities
            .FirstOrDefaultAsync(d => d.Id == activityId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return false;

        _db.TravelActivities.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteAllByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var rows = await _db.TravelActivities
            .Where(a => a.TravelPlanId == travelPlanId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return 0;

        _db.TravelActivities.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private Task<bool> PlanExistsAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        _travelPlans.ExistsAsync(travelPlanId, cancellationToken);

    private static void ValidateAgainstPlan(DateOnly activityDate, TravelPlanMetaDto plan)
    {
        if (activityDate < DateContract.ToDateOnly(plan.StartDate) || activityDate > DateContract.ToDateOnly(plan.EndDate))
            throw new ArgumentException("Datum aktivnosti mora biti u okviru datuma plana putovanja.");
    }

    private static void ValidateEstimatedCost(decimal estimatedCost)
    {
        if (estimatedCost < 0)
            throw new ArgumentException("Procijenjeni trošak ne može biti negativan.");
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        if (!ActivityStatuses.Allowed.Contains(normalized))
            throw new ArgumentException("Status mora biti: planned, reserved, completed ili cancelled.");
        return normalized;
    }

    private static TravelActivityResponseDto Map(TravelActivityEntity e) =>
        new()
        {
            Id = e.Id,
            TravelPlanId = e.TravelPlanId,
            Name = e.Name,
            ActivityDate = DateContract.FromDateOnly(e.ActivityDate),
            ActivityTime = e.ActivityTime,
            Location = e.Location,
            Description = e.Description,
            EstimatedCost = e.EstimatedCost,
            Status = e.Status,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
}
