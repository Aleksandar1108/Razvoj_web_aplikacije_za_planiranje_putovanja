using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public NotificationsController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<UserNotificationDto>>> List(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.ListNotificationsAsync(context, cancellationToken));
    }

    [HttpGet("unread-count")]
    public Task<ActionResult<UnreadNotificationCountDto>> UnreadCount(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() => _remoting.Web1.GetUnreadNotificationCountAsync(context, cancellationToken));
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.MarkNotificationReadAsync(context, notificationId, cancellationToken));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return await RemotingHttp.ExecuteAsync(() =>
            _remoting.Web1.MarkAllNotificationsReadAsync(context, cancellationToken));
    }
}
