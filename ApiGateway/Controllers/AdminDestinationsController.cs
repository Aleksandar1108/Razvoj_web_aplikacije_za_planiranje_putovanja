using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminDestinationsController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public AdminDestinationsController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet("destinations")]
    public Task<ActionResult<List<AdminDestinationListItemDto>>> ListDestinations(
        CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.Destinations.ListAdminDestinationsAsync(context, cancellationToken));
    }
}
