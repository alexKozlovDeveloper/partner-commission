using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Contracts;

public sealed record ReceiveProfitEventResponse(
    string EventExternalId,
    ProfitEventStatus Status
    );
