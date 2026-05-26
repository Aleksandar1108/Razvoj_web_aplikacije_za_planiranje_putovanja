using CrossService.Clients;
using DestinationsApi.Data;
using DestinationsApi.Data.Entities;
using DestinationsApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DestinationsApi.Services;

public sealed class DestinationService : IDestinationService
{
    private readonly DestinationsDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;

    public DestinationService(DestinationsDbContext db, ITravelPlansInternalClient travelPlans)
    {
        _db = db;
        _travelPlans = travelPlans;
    }

    public async Task<IReadOnlyList<TravelDestinationResponseDto>> ListByTravelPlanIdAsync(
        Guid travelPlanId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.TravelDestinations.AsNoTracking()
            .Where(d => d.TravelPlanId == travelPlanId)
            .OrderBy(d => d.ArrivalDate)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<TravelDestinationResponseDto?> GetAsync(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        var row = await _db.TravelDestinations.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == destinationId && d.TravelPlanId == travelPlanId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TravelDestinationResponseDto> CreateAsync(
        Guid travelPlanId,
        CreateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        var plan = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken)
                   ?? throw new InvalidOperationException("Plan putovanja nije pronađen.");

        var arrival = request.ArrivalDate!.Value;
        var departure = request.DepartureDate!.Value;
        ValidateAgainstPlan(arrival, departure, plan);

        var now = DateTime.UtcNow;
        var entity = new TravelDestinationEntity
        {
            Id = Guid.NewGuid(),
            TravelPlanId = travelPlanId,
            Name = request.Name.Trim(),
            Location = request.Location.Trim(),
            ArrivalDate = arrival,
            DepartureDate = departure,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.TravelDestinations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TravelDestinationResponseDto?> UpdateAsync(
        Guid travelPlanId,
        Guid destinationId,
        UpdateTravelDestinationRequestDto request,
        CancellationToken cancellationToken)
    {
        var plan = await _travelPlans.GetMetaAsync(travelPlanId, cancellationToken);
        if (plan is null)
            return null;

        var entity = await _db.TravelDestinations
            .FirstOrDefaultAsync(d => d.Id == destinationId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return null;

        var arrival = request.ArrivalDate!.Value;
        var departure = request.DepartureDate!.Value;
        ValidateAgainstPlan(arrival, departure, plan);

        entity.Name = request.Name.Trim();
        entity.Location = request.Location.Trim();
        entity.ArrivalDate = arrival;
        entity.DepartureDate = departure;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(
        Guid travelPlanId,
        Guid destinationId,
        CancellationToken cancellationToken)
    {
        if (!await PlanExistsAsync(travelPlanId, cancellationToken))
            return false;

        var entity = await _db.TravelDestinations
            .FirstOrDefaultAsync(d => d.Id == destinationId && d.TravelPlanId == travelPlanId, cancellationToken);
        if (entity is null)
            return false;

        _db.TravelDestinations.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> PlanExistsAsync(Guid travelPlanId, CancellationToken cancellationToken) =>
        await _travelPlans.ExistsAsync(travelPlanId, cancellationToken);

    private static void ValidateAgainstPlan(DateOnly arrival, DateOnly departure, CrossService.Dtos.TravelPlanMetaDto plan)
    {
        if (departure < arrival)
            throw new ArgumentException("Datum odlaska ne može biti pre datuma dolaska.");
        if (arrival < plan.StartDate || departure > plan.EndDate)
            throw new ArgumentException("Datumi destinacije moraju biti u okviru datuma plana putovanja.");
    }

    private static TravelDestinationResponseDto Map(TravelDestinationEntity e) =>
        new()
        {
            Id = e.Id,
            TravelPlanId = e.TravelPlanId,
            Name = e.Name,
            Location = e.Location,
            ArrivalDate = e.ArrivalDate,
            DepartureDate = e.DepartureDate,
            Notes = e.Notes,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
}
