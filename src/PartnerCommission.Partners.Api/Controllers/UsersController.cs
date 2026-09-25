using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Services;

namespace PartnerCommission.Partners.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController(
    IUserService userService
    ) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateUserRequest request, CancellationToken ct) 
    {
        _ = await userService.CreateAsync(request, ct);

        return Ok();
    }

    [HttpPut("{externalId}/partner")]
    public async Task<IActionResult> SetPartnerAsync(string externalId, SetPartnerRequest request, CancellationToken ct) 
    {
        await userService.SetPartnerAsync(externalId, request, ct);

        return NoContent();
    }

    [HttpGet("{externalId}/tree/up")]
    public async Task<TreeUpResponse> GetTreeUpAsync(string externalId, CancellationToken ct) 
    {
        var result = await userService.GetTreeUpAsync(externalId, ct);

        return result;
    }

    [HttpGet("{externalId}/tree/down")]
    public async Task<TreeDownResponse> GetTreeDownAsync(string externalId, CancellationToken ct)
    {
        var result = await userService.GetTreeDownAsync(externalId, ct);

        return result;
    }
}