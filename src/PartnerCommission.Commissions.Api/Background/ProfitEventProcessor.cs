using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Shared.Hosting;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class ProfitEventProcessor(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingJobOptions> options,
    ILogger<ProfitEventProcessor> logger
    ) : PollingBackgroundService<CommissionsDbContext>(scopeFactory, options, logger)
{
    protected override long LockKey => 55;

    protected override async Task RunOnceAsync(CancellationToken ct)
    {
        List<Guid> ids;

        await using (var scope = ScopeFactory.CreateAsyncScope())
        {
            var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

            ids = await commissionsDbContext.ProfitEvents
                .Where(x => (x.Status == ProfitEventStatus.Received || x.Status == ProfitEventStatus.Unresolved)
                    && x.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .Take(Options.BatchSize)
                .ToListAsync(ct);
        }

        foreach (var id in ids)
        {
            await using var scope = ScopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<ProfitEventHandler>();

            await handler.HandleAsync(id, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        var pending = await commissionsDbContext.ProfitEvents
            .CountAsync(x => x.Status == ProfitEventStatus.Received || x.Status == ProfitEventStatus.Unresolved, ct);

        CommissionsMetrics.ProfitEventsPending.Set(pending);
    }
}
