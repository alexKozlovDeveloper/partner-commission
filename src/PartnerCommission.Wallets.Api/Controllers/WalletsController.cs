using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Shared.Pagination;
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
    public async Task<IActionResult> GetPayoutsAsync(string userExternalId, [FromQuery] PageRequest paging, CancellationToken ct)
    {
        var result = await walletsService.GetPayoutsAsync(userExternalId, paging, ct);

        return Ok(result);
    }
}