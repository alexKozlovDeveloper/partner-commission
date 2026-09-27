using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Services;

public class CommissionsService(
    CommissionsDbContext commissionsDbContext
    ) : ICommissionsService
{
    public async Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var currentSchemaType = await GetCurrentSchemaType(ct);

        var profitEvent = new ProfitEvent
        {
            Id = Guid.NewGuid(),
            UserExternalId = externalId,
            EventExternalId = request.EventExternalId,
            Profit = request.Profit,
            SchemaType = currentSchemaType,
            CreatedAtUtc = DateTime.UtcNow,            
        };

        commissionsDbContext.ProfitEvents.Add(profitEvent);

        await commissionsDbContext.SaveChangesAsync(ct);
    }

    private async Task<SchemaType> GetCurrentSchemaType(CancellationToken ct) 
    {
        var setting = await commissionsDbContext.Settings
            .Where(x => x.Key == SettingKeys.CommissionSchema)
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException();

        var value = setting.Value ?? throw new InvalidOperationException();

        if (!Enum.IsDefined(typeof(SchemaType), value))
            throw new InvalidOperationException();

        var result = (SchemaType)Enum.Parse(typeof(SchemaType), value);

        return result;
    }
}
