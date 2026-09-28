namespace PartnerCommission.Wallets.Api.Contracts;

public sealed record PayoutsResponse(
    string CommissionId,
    string EventExternalId,
    decimal Amount,
    DateTime AccruedAtUtc,
    DateTime PaidAtUtc
    );