using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Services;

namespace PartnerCommission.Commissions.Api.Controllers;

[ApiController]
[Route("users")]
public class CommissionsController(
    ICommissionsService commissionsService
    ) : ControllerBase
{
    [HttpPost("{externalId}/profit-events")]
    public async Task<IActionResult> ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        await commissionsService.ReciveProfitEventAsync(externalId, request, ct);

        return Ok();
    }

    [HttpGet("{externalId}/profit-events")]
    public async Task<IActionResult> GetProfitEventsAsync(string externalId, CancellationToken ct)
    {
        var result = await commissionsService.GetProfitEventsAsync(externalId, ct);

        return Ok(result);
    }

    [HttpGet("{externalId}/profit-events/{eventExternalId}")]
    public async Task<IActionResult> GetProfitEventAsync(string externalId, string eventExternalId, CancellationToken ct)
    {
        var result = await commissionsService.GetProfitEventAsync(externalId, eventExternalId, ct);

        return result is null 
            ? NotFound() 
            : Ok(result);
    }
}