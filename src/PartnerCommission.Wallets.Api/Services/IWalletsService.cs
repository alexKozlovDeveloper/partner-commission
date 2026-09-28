using PartnerCommission.Contracts;
using PartnerCommission.Wallets.Api.Contracts;

namespace PartnerCommission.Wallets.Api.Services;

public interface IWalletsService
{
    Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct);
    Task<IReadOnlyList<PayoutsResponse>> GetPayoutsAsync(string userExternalId, CancellationToken ct);

    // internal
    Task ReceiveCommissionAsync(CommissionAccruedMessage message, CancellationToken ct);
    Task<IReadOnlyList<CommissionPaymentResponse>> GetCommissionPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct);
}
