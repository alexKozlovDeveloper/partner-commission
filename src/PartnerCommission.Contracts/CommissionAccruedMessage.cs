namespace PartnerCommission.Contracts;

public sealed record CommissionAccruedMessage(
    Guid CommissionId,
    string EventExternalId,
    string BeneficiaryExternalId,
    decimal Amount,
    DateTime AccruedAtUtc
    )
{
    public const string MessageType = "commission.accrued";
}
