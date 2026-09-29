using PartnerCommission.Contracts;
using PartnerCommission.Shared.Pagination;
using PartnerCommission.Wallets.Api.Contracts;

namespace PartnerCommission.Wallets.Api.Services;

public interface IWalletsService
{
    Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct);
    Task<PagedResponse<PayoutsResponse>> GetPayoutsAsync(string userExternalId, PageRequest page, CancellationToken ct);

    // internal
    Task ReceiveCommissionAsync(CommissionAccruedMessage message, CancellationToken ct);
    Task<IReadOnlyList<CommissionPaymentResponse>> GetCommissionPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct);
}
