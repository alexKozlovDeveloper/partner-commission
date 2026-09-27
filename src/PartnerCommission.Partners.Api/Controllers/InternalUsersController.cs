using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Services;

namespace PartnerCommission.Partners.Api.Controllers;

[ApiController]
[Route("internal/users")]
public class InternalUsersController(
    IUserService userService
    ) : ControllerBase
{
    [HttpGet("{externalId}/ancestors")]
    public async Task<AncestorsResponse> GetAncestorsAsync(string externalId, CancellationToken ct)
    {
        var result = await userService.GetAncestorsAsync(externalId, ct);

        return result;
    }
}