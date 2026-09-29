using PartnerCommission.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

public interface IWalletsClient
{
    Task SendCommissionAccruedAsync(CommissionAccruedMessage message, CancellationToken ct);
    Task<IReadOnlyList<CommissionPaymentResponse>> GetCommissionPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct);
}
