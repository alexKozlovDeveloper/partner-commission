using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Services;

public class CommissionsService : ICommissionsService
{
    public Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var profitEvent = new ProfitEvent
        {
            Id = Guid.NewGuid(),
            UserExternalId = externalId,
            EventExternalId = request.EventExternalId,
            Profit = request.Profit,
            SchemaType = 
            CreatedAtUtc = DateTime.UtcNow,            
        };
    }
}
