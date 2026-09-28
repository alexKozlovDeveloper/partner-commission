using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Contracts;

public sealed record ProfitEventDetailsResponse(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    SchemaType SchemaType,
    ProfitEventStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    IReadOnlyList<CommissionDetailsResponse> Commissions
    );

public sealed record CommissionDetailsResponse(
    Guid CommissionId,
    string BeneficiaryExternalId,
    int Level,
    decimal Amount,
    SchemaType SchemaType,
    CommissionPaymentStatus PaymentStatus,
    DateTime? PaidAtUtc
    );

public enum CommissionPaymentStatus
{
    Pending = 0,
    Paid = 1,
    Unknown = 2
}
