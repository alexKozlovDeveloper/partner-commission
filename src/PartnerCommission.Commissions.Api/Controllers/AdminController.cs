using Microsoft.AspNetCore.Mvc;
using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Services;

namespace PartnerCommission.Commissions.Api.Controllers;

[ApiController]
[Route("admin")]
public class AdminController(
    ICommissionSchemaSettings commissionSchemaSettings
    ) : ControllerBase
{
    [HttpGet("commission-schema")]
    public async Task<IActionResult> GetCommissionSchemaAsync(CancellationToken ct)
    {
        var result = await commissionSchemaSettings.GetCurrentAsync(ct);

        return Ok(result);
    }

    [HttpPut("commission-schema")]
    public async Task<IActionResult> SetCommissionSchemaAsync(SetSchemaRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.SchemaType))
            return ValidationProblem($"Unknown schema type '{request.SchemaType}'");

        await commissionSchemaSettings.SetAsync(request.SchemaType, ct);

        return NoContent();
    }
}