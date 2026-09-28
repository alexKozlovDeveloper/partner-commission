using PartnerCommission.Wallets.Api.Contracts;

namespace PartnerCommission.Wallets.Api.Services;

public interface IWalletsService
{
    Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct);
    Task<PayoutsResponse> GetPayoutsAsync(string userExternalId, CancellationToken ct);
    Task ReciveCommissionAsync(CancellationToken ct);
}
