using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Shared.Pagination;

namespace PartnerCommission.Commissions.Api.Services;

public interface ICommissionsService
{
    Task<ReceiveProfitEventResult> ReceiveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct);
    Task<PagedResponse<ProfitEventResponse>> GetProfitEventsAsync(string externalId, PageRequest page, CancellationToken ct);
    Task<ProfitEventDetailsResponse?> GetProfitEventAsync(string externalId, string eventExternalId, CancellationToken ct);
}
