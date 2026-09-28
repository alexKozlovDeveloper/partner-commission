namespace PartnerCommission.Wallets.Api.Contracts;

public sealed record PayoutsResponse(
    Guid CommissionId,
    string EventExternalId,
    decimal Amount,
    DateTime AccruedAtUtc,
    DateTime? PaidAtUtc
    );