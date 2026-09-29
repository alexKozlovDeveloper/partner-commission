namespace PartnerCommission.Contracts;

public sealed record CommissionPaymentResponse(
    Guid CommissionId,
    DateTime? PaidAtUtc
    );
