using CrossService.Clients;
using CrossService.Dtos;
using DestinationsApi.Data;
using DestinationsApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DestinationsApi.Services;

public sealed class AdminDestinationService : IAdminDestinationService
{
    private readonly DestinationsDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;
    private readonly IWeb1InternalClient _web1;

    public AdminDestinationService(
        DestinationsDbContext db,
        ITravelPlansInternalClient travelPlans,
        IWeb1InternalClient web1)
    {
        _db = db;
        _travelPlans = travelPlans;
        _web1 = web1;
    }

    public async Task<IReadOnlyList<AdminDestinationListItemDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var destinations = await _db.TravelDestinations.AsNoTracking()
            .OrderBy(d => d.ArrivalDate)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var planIds = destinations.Select(d => d.TravelPlanId).Distinct().ToList();
        var plans = (await _travelPlans.GetMetaBatchAsync(planIds, cancellationToken))
            .ToDictionary(p => p.Id);
        var userIds = plans.Values.Select(p => p.UserId).Distinct().ToList();
        var users = await _web1.GetUsersBriefAsync(userIds, cancellationToken);

        return destinations.Select(d =>
        {
            plans.TryGetValue(d.TravelPlanId, out var plan);
            UserBriefDto? user = null;
            if (plan is not null)
                users.TryGetValue(plan.UserId, out user);

            return new AdminDestinationListItemDto
            {
                Id = d.Id,
                TravelPlanId = d.TravelPlanId,
                TravelPlanName = plan?.Name ?? "Plan",
                OwnerUserId = plan?.UserId ?? Guid.Empty,
                OwnerEmail = user?.Email ?? string.Empty,
                OwnerDisplayName = user?.DisplayName ?? "Korisnik",
                Name = d.Name,
                Location = d.Location,
                ArrivalDate = d.ArrivalDate,
                DepartureDate = d.DepartureDate,
                Notes = d.Notes,
                CreatedAtUtc = d.CreatedAtUtc,
                UpdatedAtUtc = d.UpdatedAtUtc
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminTravelPlanOptionDto>> ListTravelPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var planIds = await _db.TravelDestinations.AsNoTracking()
            .Select(d => d.TravelPlanId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var plans = await _travelPlans.GetMetaBatchAsync(planIds, cancellationToken);
        var userIds = plans.Select(p => p.UserId).Distinct().ToList();
        var users = await _web1.GetUsersBriefAsync(userIds, cancellationToken);

        return plans
            .OrderBy(p => p.Name)
            .Select(p =>
            {
                users.TryGetValue(p.UserId, out var user);
                return new AdminTravelPlanOptionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    OwnerUserId = p.UserId,
                    OwnerEmail = user?.Email ?? string.Empty,
                    OwnerDisplayName = user?.DisplayName ?? "Korisnik",
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                };
            })
            .ToList();
    }
}
