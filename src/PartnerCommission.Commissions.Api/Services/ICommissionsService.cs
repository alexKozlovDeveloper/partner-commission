using PartnerCommission.Commissions.Api.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

public interface ICommissionsService
{
    Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct);
}
