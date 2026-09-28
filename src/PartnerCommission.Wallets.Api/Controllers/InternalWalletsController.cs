using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Wallets.Api.Services;

namespace PartnerCommission.Wallets.Api.Controllers;

[ApiController]
[Route("internal")]
public class InternalWalletsController(
    IWalletsService walletsService
    ) : ControllerBase
{
    [HttpPost("commissions/{commissionId}")]
    public async Task<IActionResult> ReciveCommissionAsync(string commissionId, CancellationToken ct)
    {
        await walletsService.ReciveCommissionAsync(ct);

        return Ok();
    }
}