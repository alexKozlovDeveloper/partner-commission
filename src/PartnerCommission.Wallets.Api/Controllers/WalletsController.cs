using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Wallets.Api.Services;

namespace PartnerCommission.Wallets.Api.Controllers;

[ApiController]
[Route("users")]
public class WalletsController(
    IWalletsService walletsService
    ) : ControllerBase
{
    [HttpGet("{userExternalId}/wallet")]
    public async Task<IActionResult> GetWalletAsync(string userExternalId, CancellationToken ct)
    {
        var result = await walletsService.GetWalletAsync(userExternalId, ct);

        return Ok(result);
    }

    [HttpGet("{userExternalId}/wallet/payouts")]
    public async Task<IActionResult> GetPayoutsAsync(string userExternalId, CancellationToken ct)
    {
        var result = await walletsService.GetPayoutsAsync(userExternalId, ct);

        return Ok(result);
    }
}