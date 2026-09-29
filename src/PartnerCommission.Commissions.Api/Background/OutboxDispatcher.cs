using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Shared.Hosting;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingJobOptions> options,
    ILogger<OutboxDispatcher> logger
    ) : PollingBackgroundService<CommissionsDbContext>(scopeFactory, options, logger)
{
    protected override long LockKey => 54;

    protected override async Task RunOnceAsync(CancellationToken ct)
    {
        await UpdatePendingMetricAsync(ct);

        List<Guid> ids;

        await using (var scope = ScopeFactory.CreateAsyncScope())
        {
            var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

            ids = await commissionsDbContext.OutboxMessages
                .Where(x => x.ProcessedAtUtc == null && x.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .Take(Options.BatchSize)
                .ToListAsync(ct);
        }

        foreach (var id in ids)
        {
            await using var scope = ScopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<OutboxMessageHandler>();

            await handler.HandleAsync(id, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        var pending = await commissionsDbContext.OutboxMessages
            .CountAsync(x => x.ProcessedAtUtc == null, ct);

        CommissionsMetrics.OutboxPending.Set(pending);
    }
}
