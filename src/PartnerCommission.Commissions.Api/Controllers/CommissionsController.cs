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
    public async Task<IActionResult> ReceiveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var result = await commissionsService.ReceiveProfitEventAsync(externalId, request, ct);

        var response = new ReceiveProfitEventResponse(request.EventExternalId, result.Status);

        return result.Duplicate
            ? Ok(response)
            : Accepted($"/users/{Uri.EscapeDataString(externalId)}/profit-events/{Uri.EscapeDataString(request.EventExternalId)}", response);
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