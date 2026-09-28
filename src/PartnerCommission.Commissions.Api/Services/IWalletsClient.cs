using PartnerCommission.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

public interface IWalletsClient
{
    Task SendCommissionAccruedAsync(CommissionAccruedMessage message, CancellationToken ct);
}
