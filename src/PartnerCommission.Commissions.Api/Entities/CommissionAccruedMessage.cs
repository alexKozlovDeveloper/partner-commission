namespace PartnerCommission.Commissions.Api.Entities;

public sealed record CommissionAccruedMessage(
    Guid CommissionId,
    string EventExternalId,
    string BeneficiaryExternalId,
    decimal Amount,
    DateTime AccruedAtUtc
    );