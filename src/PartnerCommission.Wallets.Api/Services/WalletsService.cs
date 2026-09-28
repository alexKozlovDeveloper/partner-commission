using PartnerCommission.Wallets.Api.Contracts;

namespace PartnerCommission.Wallets.Api.Services;

public class WalletsService : IWalletsService
{
    public Task<PayoutsResponse> GetPayoutsAsync(string userExternalId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task ReciveCommissionAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
