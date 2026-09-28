using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Services;
using PartnerCommission.Shared.Pagination;

namespace PartnerCommission.Partners.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController(
    IUserService userService
    ) : ControllerBase
{
    [HttpGet("{externalId}")]
    public async Task<IActionResult> GetUserAsync(string externalId, CancellationToken ct)
    {
        var result = await userService.GetAsync(externalId, ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateUserRequest request, CancellationToken ct) 
    {
        var result = await userService.CreateAsync(request, ct);

        var response = new CreateUserResponse(result.Id, request.ExternalId);

        return result.Duplicate
            ? Ok(response)
            : Created($"/users/{Uri.EscapeDataString(request.ExternalId)}", response);
    }

    [HttpGet]
    public async Task<IActionResult> ListUsersAsync([FromQuery] PageRequest paging, CancellationToken ct)
    {
        var users = await userService.ListAsync(paging, ct);

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