using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Services;

public sealed record ReceiveProfitEventResult(
    ProfitEventStatus Status,
    bool Duplicate
    );
