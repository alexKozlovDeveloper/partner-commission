using Microsoft.EntityFrameworkCore;
using PartnerCommission.Shared.Data;
using PartnerCommission.Wallets.Api.Data;
using PartnerCommission.Wallets.Api.Entities;
using PartnerCommission.Wallets.Api.Observability;
using Prometheus;

namespace PartnerCommission.Wallets.Api.Background;

internal sealed class PayoutProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<PayoutProcessor> logger
    ) : BackgroundService
{
    // TODO: move to app config
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 50;

    private const long PayoutLockKey = 53;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                await PayoutPendingAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payout loop failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task PayoutPendingAsync(CancellationToken ct)
    {
        await using var lockScope = scopeFactory.CreateAsyncScope();

        var lockDbContext = lockScope.ServiceProvider.GetRequiredService<WalletsDbContext>();

        await using var payoutLock = await AdvisoryLock.TryAcquireAsync(lockDbContext, PayoutLockKey, ct);

        if (payoutLock is null)
        {
            logger.LogDebug("Payout is running on another instance, skipping tick");
            return;
        }

        using var timer = WalletsMetrics.PayoutRunSeconds.NewTimer();

        List<string> userExternalIds;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var walletsDbContext = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();

            userExternalIds = await walletsDbContext.WalletEntries
                .Where(x => x.Status == WalletEntryStatus.Pending)
                .Select(x => x.UserExternalId)
                .Distinct()
                .OrderBy(x => x)
                .Take(BatchSize)
                .ToListAsync(ct);
        }

        foreach (var userExternalId in userExternalIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<PayoutHandler>();

            await handler.HandleAsync(userExternalId, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var walletsDbContext = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();

        var pending = await walletsDbContext.WalletEntries
            .CountAsync(x => x.Status == WalletEntryStatus.Pending, ct);

        WalletsMetrics.WalletEntriesPending.Set(pending);
    }
}
