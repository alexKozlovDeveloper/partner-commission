using PartnerCommission.Contracts;
using PartnerCommission.Wallets.Api.Contracts;

namespace PartnerCommission.Wallets.Api.Services;

public interface IWalletsService
{
    Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct);
    Task<IReadOnlyList<PayoutsResponse>> GetPayoutsAsync(string userExternalId, CancellationToken ct);
    Task ReceiveCommissionAsync(CommissionAccruedMessage message, CancellationToken ct);
}
