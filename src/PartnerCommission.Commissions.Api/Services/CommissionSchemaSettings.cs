using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Services;

internal sealed class CommissionSchemaSettings(
    CommissionsDbContext commissionsDbContext
    ) : ICommissionSchemaSettings
{
    public async Task<SchemaType> GetCurrentAsync(CancellationToken ct)
    {
        var schemaTypeSetting = await commissionsDbContext.Settings
            .Where(x => x.Key == SettingKeys.CommissionSchema)
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException($"Setting '{SettingKeys.CommissionSchema}' is missing");

        var value = schemaTypeSetting.Value;

        return Enum.TryParse<SchemaType>(value, ignoreCase: true, out var schema)
            ? schema
            : throw new InvalidOperationException($"Invalid value '{value}' for '{SettingKeys.CommissionSchema}'");
    }

    public async Task SetAsync(SchemaType schema, CancellationToken ct)
    {
        var schemaTypeSetting = await commissionsDbContext.Settings
            .Where(x => x.Key == SettingKeys.CommissionSchema)
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException($"Setting '{SettingKeys.CommissionSchema}' is missing");

        schemaTypeSetting.Value = schema.ToString();

        await commissionsDbContext.SaveChangesAsync(ct);
    }
}
