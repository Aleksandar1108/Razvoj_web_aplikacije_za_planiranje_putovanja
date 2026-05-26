using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web1.Dtos.Notifications;
using Web1.Services.Notifications;

namespace Web1.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    private Guid? ActingUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserNotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserNotificationDto>>> List(CancellationToken cancellationToken)
    {
        var userId = ActingUserId();
        if (userId is null)
            return Unauthorized();

        return Ok(await _notifications.ListForUserAsync(userId.Value, cancellationToken));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationCountDto>> UnreadCount(CancellationToken cancellationToken)
    {
        var userId = ActingUserId();
        if (userId is null)
            return Unauthorized();

        var count = await _notifications.GetUnreadCountAsync(userId.Value, cancellationToken);
        return Ok(new UnreadNotificationCountDto { Count = count });
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = ActingUserId();
        if (userId is null)
            return Unauthorized();

        var ok = await _notifications.MarkReadAsync(userId.Value, notificationId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = ActingUserId();
        if (userId is null)
            return Unauthorized();

        await _notifications.MarkAllReadAsync(userId.Value, cancellationToken);
        return NoContent();
    }
}
