using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CrossService.Clients;
using CrossService.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlansApi.Data;
using TravelPlansApi.Dtos;
using TravelPlansApi.Services;

namespace TravelPlansApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminTravelPlansController : ControllerBase
{
    private readonly TravelPlansDbContext _db;
    private readonly ITravelPlanService _plans;
    private readonly IAdminPlanNotificationService _adminNotifications;
    private readonly IWeb1InternalClient _web1;

    public AdminTravelPlansController(
        TravelPlansDbContext db,
        ITravelPlanService plans,
        IAdminPlanNotificationService adminNotifications,
        IWeb1InternalClient web1)
    {
        _db = db;
        _plans = plans;
        _adminNotifications = adminNotifications;
        _web1 = web1;
    }

    [HttpGet("travel-plans")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTravelPlanListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminTravelPlanListItemDto>>> List(CancellationToken cancellationToken)
    {
        var plans = await _db.TravelPlans.AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var userIds = plans.Select(p => p.UserId).Distinct().ToList();
        var users = await _web1.GetUsersBriefAsync(userIds, cancellationToken);

        var rows = plans.Select(p =>
        {
            users.TryGetValue(p.UserId, out var user);
            return new AdminTravelPlanListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                OwnerUserId = p.UserId,
                OwnerEmail = user?.Email ?? string.Empty,
                OwnerDisplayName = user?.DisplayName ?? "Korisnik",
                StartDate = p.StartDate,
                EndDate = p.EndDate
            };
        }).ToList();

        return Ok(rows);
    }

    [HttpPost("travel-plans")]
    [ProducesResponseType(typeof(TravelPlanResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPlanResponseDto>> Create(
        [FromBody] AdminCreateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!await _web1.UserExistsAsync(request.OwnerUserId, cancellationToken))
            return NotFound(new { message = "Korisnik nije pronađen." });

        var adminId = ActingUserId();
        if (adminId is null)
            return Unauthorized();

        var createDto = new CreateTravelPlanRequestDto
        {
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlannedBudget = request.PlannedBudget,
            GeneralNotes = request.GeneralNotes
        };

        try
        {
            var created = await _plans.CreateAsync(request.OwnerUserId, createDto, cancellationToken);

            if (request.OwnerUserId != adminId.Value)
            {
                await _adminNotifications.NotifyPlanOwnerAsync(
                    adminId.Value,
                    created.Id,
                    AdminNotificationCategories.PlanBasic,
                    AdminMutationAction.Created,
                    cancellationToken: cancellationToken);
            }

            return Created($"/api/v1/travel-plans/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? ActingUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
