using ApiGateway.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts.Dtos;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/admin/travel-plans")]
[Authorize(Roles = "Admin")]
public sealed class AdminTravelPlansController : ControllerBase
{
    private readonly RemotingServices _remoting;

    public AdminTravelPlansController(RemotingServices remoting)
    {
        _remoting = remoting;
    }

    [HttpGet]
    public Task<ActionResult<List<AdminTravelPlanListItemDto>>> List(CancellationToken cancellationToken)
    {
        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        return RemotingHttp.ExecuteAsync(() =>
            _remoting.TravelPlans.ListAdminTravelPlansAsync(context, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreateTravelPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var context = ServiceCallContextFactory.FromHttpContext(HttpContext);
        var result = await RemotingHttp.ExecuteAsync(() =>
            _remoting.TravelPlans.AdminCreateTravelPlanAsync(context, request, cancellationToken));

        if (result.Result is not null)
            return result.Result;

        return Created($"/api/v1/travel-plans/{result.Value!.Id}", result.Value);
    }
}
