using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlansApi.Data;
using CrossService.Dtos;

namespace TravelPlansApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/internal/travel-plans")]
public sealed class InternalTravelPlansController : ControllerBase
{
    private readonly TravelPlansDbContext _db;

    public InternalTravelPlansController(TravelPlansDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("{travelPlanId:guid}/meta")]
    public async Task<ActionResult<TravelPlanMetaDto>> Meta(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var p = await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == travelPlanId, cancellationToken);
        if (p is null)
            return NotFound();

        return Ok(new TravelPlanMetaDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            PlannedBudget = p.PlannedBudget
        });
    }

    [HttpGet("{travelPlanId:guid}/exists")]
    [AllowAnonymous]
    public async Task<ActionResult<TravelPlanExistsDto>> Exists(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var exists = await _db.TravelPlans.AsNoTracking()
            .AnyAsync(x => x.Id == travelPlanId, cancellationToken);
        return Ok(new TravelPlanExistsDto { Exists = exists });
    }

    [HttpGet("{travelPlanId:guid}/owner")]
    public async Task<ActionResult<TravelPlanOwnerDto>> Owner(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var p = await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == travelPlanId, cancellationToken);
        if (p is null)
            return Ok(new TravelPlanOwnerDto { IsOwner = false });

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return Ok(new TravelPlanOwnerDto
        {
            IsOwner = p.UserId == userId.Value,
            OwnerUserId = p.UserId
        });
    }

    /// <summary>
    /// Batch meta za interne pozive (SharingApi lista deljenih planova, admin destinacije).
    /// Zahteva JWT; ID-jeve planova šalje pozivajući servis (npr. samo planovi iz share tabele).
    /// </summary>
    [HttpPost("meta-batch")]
    public async Task<ActionResult<IReadOnlyList<TravelPlanMetaDto>>> MetaBatch(
        [FromBody] SharedPlanMetaBatchRequestDto request,
        CancellationToken cancellationToken)
    {
        var ids = request.TravelPlanIds?.Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
            return Ok(Array.Empty<TravelPlanMetaDto>());

        var rows = await _db.TravelPlans.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new TravelPlanMetaDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PlannedBudget = p.PlannedBudget
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
