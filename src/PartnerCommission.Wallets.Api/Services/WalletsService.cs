using Microsoft.EntityFrameworkCore;
using PartnerCommission.Contracts;
using PartnerCommission.Shared.Exceptions;
using PartnerCommission.Shared.Pagination;
using PartnerCommission.Wallets.Api.Contracts;
using PartnerCommission.Wallets.Api.Data;
using PartnerCommission.Wallets.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Wallets.Api.Services;

public class WalletsService(
    WalletsDbContext walletsDbContext
    ) : IWalletsService
{
    public async Task<WalletResponse> GetWalletAsync(string userExternalId, CancellationToken ct)
    {
        var wallet = await walletsDbContext.Wallets
            .Where(x => x.UserExternalId == userExternalId)
            .FirstOrDefaultAsync(ct);

        var result = new WalletResponse(wallet?.Balance ?? 0);

        return result;
    }

    public async Task<PagedResponse<PayoutsResponse>> GetPayoutsAsync(string userExternalId, PageRequest page, CancellationToken ct)
    {
        var result = await walletsDbContext.WalletEntries
            .Where(x => x.UserExternalId == userExternalId)
            .OrderByDescending(x => x.AccruedAtUtc)
                .ThenByDescending(x => x.CommissionId)
            .Select(x => new PayoutsResponse(
                x.CommissionId,
                x.EventExternalId,
                x.Amount,
                x.AccruedAtUtc,
                x.PaidAtUtc
                ))
            .ToPagedAsync(page, ct);

        return result;
    }

    public async Task<IReadOnlyList<CommissionPaymentResponse>> GetCommissionPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct)
    {
        var result = await walletsDbContext.WalletEntries
            .Where(x => commissionIds.Contains(x.CommissionId))
            .Select(x => new CommissionPaymentResponse(
                x.CommissionId,
                x.PaidAtUtc
                ))
            .ToListAsync(ct);

        return result;
    }

    public async Task ReceiveCommissionAsync(CommissionAccruedMessage message, CancellationToken ct)
    {
        if (message.Amount <= 0)
            throw new ValidationException("Amount must be positive");

        var existing = await walletsDbContext.WalletEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CommissionId == message.CommissionId, ct);

        if (existing is not null)
        {
            if (existing.Amount != message.Amount || existing.UserExternalId != message.BeneficiaryExternalId)
                throw new ConflictException($"Commission {message.CommissionId} already received with different data");

            return;
        }

        walletsDbContext.WalletEntries.Add(new WalletEntry
        {
            CommissionId = message.CommissionId,
            UserExternalId = message.BeneficiaryExternalId,
            EventExternalId = message.EventExternalId,
            Amount = message.Amount,
            Status = WalletEntryStatus.Pending,
            AccruedAtUtc = message.AccruedAtUtc,
            ReceivedAtUtc = DateTime.UtcNow
        });

        await walletsDbContext.SaveChangesAsync(ct);
    }
}
