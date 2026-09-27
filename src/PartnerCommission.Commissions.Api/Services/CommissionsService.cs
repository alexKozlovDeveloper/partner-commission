using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Services;

public class CommissionsService(
    CommissionsDbContext commissionsDbContext,
    ICommissionSchemaSettings commissionSchemaSettings
    ) : ICommissionsService
{
    public async Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var currentSchemaType = await commissionSchemaSettings.GetCurrentAsync(ct);

        var profitEvent = new ProfitEvent
        {
            Id = Guid.NewGuid(),
            Status = ProfitEventStatus.Received,
            UserExternalId = externalId,
            EventExternalId = request.EventExternalId,
            Profit = request.Profit,
            SchemaType = currentSchemaType,
            CreatedAtUtc = DateTime.UtcNow,
            Attempts = 0,
            NextAttemptAtUtc = DateTime.UtcNow            
        };

        commissionsDbContext.ProfitEvents.Add(profitEvent);

        await commissionsDbContext.SaveChangesAsync(ct);
    }
}
