using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web1.Data;
using Web1.Data.Entities;
using CrossService.Clients;
using CrossService.Dtos;
using Web1.Services.Admin;

namespace Web1.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/internal")]
public sealed class InternalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;

    public InternalController(AppDbContext db, ITravelPlansInternalClient travelPlans)
    {
        _db = db;
        _travelPlans = travelPlans;
    }

    [HttpGet("users/{userId:guid}/exists")]
    public async Task<ActionResult<UserExistsDto>> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken);
        return Ok(new UserExistsDto { Exists = exists });
    }

    [HttpPost("users/brief")]
    public async Task<ActionResult<IReadOnlyList<UserBriefDto>>> UsersBrief(
        [FromBody] UsersBriefRequestDto request,
        CancellationToken cancellationToken)
    {
        var ids = request.UserIds?.Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
            return Ok(Array.Empty<UserBriefDto>());

        var users = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserBriefDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                DisplayName = DisplayName(u.FirstName, u.LastName)
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("notifications")]
    public async Task<IActionResult> CreateNotification(
        [FromBody] CreateAdminNotificationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var meta = await _travelPlans.GetMetaAsync(request.TravelPlanId, cancellationToken);
        if (meta is null)
            return NotFound();

        var planName = string.IsNullOrWhiteSpace(meta.Name) ? "putovanje" : meta.Name.Trim();
        var (title, message) = AdminNotificationMessageBuilder.Build(
            request.Category,
            request.Action,
            planName,
            request.ItemLabel,
            request.ChecklistDone);

        _db.UserNotifications.Add(new UserNotificationEntity
        {
            Id = Guid.NewGuid(),
            UserId = meta.UserId,
            Category = request.Category,
            Title = title,
            Message = message,
            TravelPlanId = request.TravelPlanId,
            TravelDestinationId = request.RelatedEntityId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string DisplayName(string first, string last)
    {
        var parts = new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim());
        var name = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(name) ? "Korisnik" : name;
    }
}
