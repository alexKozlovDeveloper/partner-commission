using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerCommission.Shared.Data;

namespace PartnerCommission.Shared.Hosting;

public abstract class PollingBackgroundService<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingJobOptions> options,
    ILogger logger
    ) : BackgroundService
    where TDbContext : DbContext
{
    protected IServiceScopeFactory ScopeFactory { get; } = scopeFactory;

    protected PollingJobOptions Options => options.Get(JobName);

    protected string JobName => GetType().Name;

    protected abstract long LockKey { get; }

    protected abstract Task RunOnceAsync(CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Options.PollInterval);

        do
        {
            try
            {
                await RunWithLockAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Job} tick failed", JobName);
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunWithLockAsync(CancellationToken ct)
    {
        await using var lockScope = ScopeFactory.CreateAsyncScope();

        var lockDbContext = lockScope.ServiceProvider.GetRequiredService<TDbContext>();

        await using var jobLock = await AdvisoryLock.TryAcquireAsync(lockDbContext, LockKey, ct);

        if (jobLock is null)
        {
            logger.LogDebug("{Job} is running on another instance, skipping tick", JobName);
            return;
        }

        await RunOnceAsync(ct);
    }
}

public static class PollingJobServiceCollectionExtensions
{
    public static IServiceCollection AddPollingJob<TJob>(this IServiceCollection services)
        where TJob : class, IHostedService
    {
        var name = typeof(TJob).Name;

        services.AddOptions<PollingJobOptions>(name)
            .BindConfiguration($"{PollingJobOptions.Section}:{name}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<TJob>();

        return services;
    }
}
