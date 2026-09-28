using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PartnerCommission.Shared.Data;

public static class MigrationExtensions
{
    private const long MigrationLockKey = 52;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static async Task MigrateWithLockAsync<TContext>(this IServiceProvider services, TimeSpan? timeout = null)
        where TContext : DbContext
    {
        using var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        var ct = cts.Token;

        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        AdvisoryLock? migrationLock;

        while ((migrationLock = await AdvisoryLock.TryAcquireAsync(db, MigrationLockKey, ct)) is null)
        {
            logger.LogInformation("Migration lock is held by another instance, waiting...");

            await Task.Delay(RetryDelay, ct);
        }

        await using (migrationLock)
        {
            var database = db.Database.GetDbConnection().Database;

            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database {Database} is up to date", database);
                return;
            }

            logger.LogInformation("Applying {Count} migration(s) to {Database}: {Migrations}",
                pending.Count, database, string.Join(", ", pending));

            await db.Database.MigrateAsync(ct);

            logger.LogInformation("Migrations applied to {Database}", database);
        }
    }
}
