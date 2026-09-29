using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Shared.Data;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger
    ) : BackgroundService
{
    // TODO: move to app config
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    private const long DispatchLockKey = 54;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                await DispatchPendingAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch loop failed");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        await using var lockScope = scopeFactory.CreateAsyncScope();

        var lockDbContext = lockScope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        await using var dispatchLock = await AdvisoryLock.TryAcquireAsync(lockDbContext, DispatchLockKey, ct);

        if (dispatchLock is null)
        {
            logger.LogDebug("Outbox is dispatched by another instance, skipping tick");
            return;
        }

        List<Guid> ids;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

            ids = await commissionsDbContext.OutboxMessages
                .Where(x => x.ProcessedAtUtc == null && x.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(ct);
        }

        foreach (var id in ids)
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider.GetRequiredService<OutboxMessageHandler>();

            await handler.HandleAsync(id, ct);
        }

        await UpdatePendingMetricAsync(ct);
    }

    private async Task UpdatePendingMetricAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var commissionsDbContext = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();

        var pending = await commissionsDbContext.OutboxMessages
            .CountAsync(x => x.ProcessedAtUtc == null, ct);

        CommissionsMetrics.OutboxPending.Set(pending);
    }
}
