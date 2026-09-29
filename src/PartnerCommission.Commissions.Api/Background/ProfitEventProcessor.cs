using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Shared.Data;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class ProfitEventProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<ProfitEventProcessor> logger
    ) : BackgroundService
{
    // TODO: move to app config
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    private const long ProcessLockKey = 55;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                await ProcessPendingAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Profit event processing loop failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        await using var lockScope = scopeFactory.CreateAsyncScope();

        var lockDbContext = lockScope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        await using var processLock = await AdvisoryLock.TryAcquireAsync(lockDbContext, ProcessLockKey, ct);

        if (processLock is null)
        {
            logger.LogDebug("Profit events are processed by another instance, skipping tick");
            return;
        }

        List<Guid> ids;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

            ids = await commissionsDbContext.ProfitEvents
                .Where(x => (x.Status == ProfitEventStatus.Received || x.Status == ProfitEventStatus.Unresolved)
                    && x.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(ct);
        }

        foreach (var id in ids)
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<ProfitEventHandler>();

            await handler.HandleAsync(id, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        var pending = await commissionsDbContext.ProfitEvents
            .CountAsync(x => x.Status == ProfitEventStatus.Received || x.Status == ProfitEventStatus.Unresolved, ct);

        CommissionsMetrics.ProfitEventsPending.Set(pending);
    }
}