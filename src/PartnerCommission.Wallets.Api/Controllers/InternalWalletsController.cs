using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Contracts;
using PartnerCommission.Wallets.Api.Services;

namespace PartnerCommission.Wallets.Api.Controllers;

[ApiController]
[Route("internal")]
public class InternalWalletsController(
    IWalletsService walletsService
    ) : ControllerBase
{
    [HttpPost("commissions")]
    public async Task<IActionResult> ReceiveCommissionAsync(CommissionAccruedMessage message, CancellationToken ct)
    {
        await walletsService.ReceiveCommissionAsync(message, ct);

        return NoContent();
    }
}