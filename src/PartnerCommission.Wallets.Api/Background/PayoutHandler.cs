using Microsoft.EntityFrameworkCore;
using PartnerCommission.Wallets.Api.Data;
using PartnerCommission.Wallets.Api.Entities;

namespace PartnerCommission.Wallets.Api.Background;

internal sealed class PayoutHandler(
    WalletsDbContext walletsDbContext,
    ILogger<PayoutHandler> logger
    )
{
    public async Task HandleAsync(string userExternalId, CancellationToken ct)
    {
        using var _ = logger.BeginScope(new Dictionary<string, object>
        {
            ["UserExternalId"] = userExternalId
        });

        try
        {
            await ProcessAsync(userExternalId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Payout failed, will retry on next run");
        }
    }

    private async Task ProcessAsync(string userExternalId, CancellationToken ct)
    {
        var entries = await walletsDbContext.WalletEntries
            .Where(x => x.UserExternalId == userExternalId && x.Status == WalletEntryStatus.Pending)
            .ToListAsync(ct);

        if (entries.Count == 0)
            return;

        var now = DateTime.UtcNow;

        var wallet = await walletsDbContext.Wallets
            .Where(x => x.UserExternalId == userExternalId)
            .SingleOrDefaultAsync(ct);

        if (wallet is null)
        {
            wallet = new Wallet
            {
                UserExternalId = userExternalId,
                Balance = 0,
                UpdatedAtUtc = now
            };

            walletsDbContext.Wallets.Add(wallet);
        }

        foreach (var entry in entries)
        {
            entry.Status = WalletEntryStatus.Paid;
            entry.PaidAtUtc = now;
        }

        var total = entries.Sum(x => x.Amount);

        wallet.Balance += total;
        wallet.UpdatedAtUtc = now;

        await walletsDbContext.SaveChangesAsync(ct);

        logger.LogInformation("Paid out {Count} commissions, total added amount {Total}, new balance {Balance}", entries.Count, total, wallet.Balance);
    }
}
