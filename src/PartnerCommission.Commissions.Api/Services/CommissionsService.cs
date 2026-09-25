using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Services;

public class CommissionsService(
    CommissionsDbContext commissionsDbContext
    ) : ICommissionsService
{
    public async Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var profitEvent = new ProfitEvent
        {
            Id = Guid.NewGuid(),
            UserExternalId = externalId,
            EventExternalId = request.EventExternalId,
            Profit = request.Profit,
            SchemaType = Domain.SchemaType.Linear, // TODO: stub
            CreatedAtUtc = DateTime.UtcNow,            
        };

        commissionsDbContext.ProfitEvents.Add(profitEvent);

        await commissionsDbContext.SaveChangesAsync(ct);
    }
}
