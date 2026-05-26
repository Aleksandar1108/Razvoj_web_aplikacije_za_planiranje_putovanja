using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web1.Data;

namespace Web1.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/travel-plans/{travelPlanId:guid}")]
public sealed class InternalTravelPlanCascadeController : ControllerBase
{
    private readonly AppDbContext _db;

    public InternalTravelPlanCascadeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpDelete("cascade")]
    public async Task<IActionResult> CascadeDelete(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var rows = await _db.UserNotifications
            .Where(n => n.TravelPlanId == travelPlanId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return Ok(new { deleted = 0 });

        _db.UserNotifications.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { deleted = rows.Count });
    }
}
