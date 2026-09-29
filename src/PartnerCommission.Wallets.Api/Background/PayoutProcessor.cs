using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PartnerCommission.Shared.Hosting;
using PartnerCommission.Wallets.Api.Data;
using PartnerCommission.Wallets.Api.Entities;
using PartnerCommission.Wallets.Api.Observability;
using Prometheus;

namespace PartnerCommission.Wallets.Api.Background;

internal sealed class PayoutProcessor(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingJobOptions> options,
    ILogger<PayoutProcessor> logger
    ) : PollingBackgroundService<WalletsDbContext>(scopeFactory, options, logger)
{
    protected override long LockKey => 53;

    protected override async Task RunOnceAsync(CancellationToken ct)
    {
        using var timer = WalletsMetrics.PayoutRunSeconds.NewTimer();

        List<string> userExternalIds;

        await using (var scope = ScopeFactory.CreateAsyncScope())
        {
            var walletsDbContext = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();

            userExternalIds = await walletsDbContext.WalletEntries
                .Where(x => x.Status == WalletEntryStatus.Pending)
                .Select(x => x.UserExternalId)
                .Distinct()
                .OrderBy(x => x)
                .Take(Options.BatchSize)
                .ToListAsync(ct);
        }

        foreach (var userExternalId in userExternalIds)
        {
            await using var scope = ScopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<PayoutHandler>();

            await handler.HandleAsync(userExternalId, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var walletsDbContext = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();

        var pending = await walletsDbContext.WalletEntries
            .CountAsync(x => x.Status == WalletEntryStatus.Pending, ct);

        WalletsMetrics.WalletEntriesPending.Set(pending);
    }
}
