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
        var id = await userService.CreateAsync(request, ct);

        //return Created($"/users/{request.ExternalId}", new { id, request.ExternalId });
        return Created();
    }

    [HttpGet]
    public async Task<IActionResult> ListUsersAsync(CancellationToken ct)
    {
        var users = await userService.ListAsync(ct);

        return Ok(users);
    }

    [HttpPut("{externalId}/partner")]
    public async Task<IActionResult> SetPartnerAsync(string externalId, SetPartnerRequest request, CancellationToken ct) 
    {
        await userService.SetPartnerAsync(externalId, request, ct);

        return NoContent();
    }

    [HttpDelete("{externalId}/partner")]
    public async Task<IActionResult> DeletePartnerAsync(string externalId, CancellationToken ct)
    {
        await userService.DeletePartnerAsync(externalId, ct);

        return NoContent();
    }

    [HttpGet("{externalId}/tree")]
    public async Task<IActionResult> GetPartnersTreeAsync(string externalId, CancellationToken ct)
    {
        var result = await userService.GetPartnersTreeAsync(externalId, ct);

        return Ok(result);
    }
}