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
    [HttpPost("{externalId}/events")]
    public async Task<IActionResult> ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        await commissionsService.ReciveProfitEventAsync(externalId, request, ct);

        return Ok();
    }
}