using System.Data.Common;
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

        await db.Database.OpenConnectionAsync(ct);

        try
        {
            var connection = db.Database.GetDbConnection();

            while (!await TryAcquireLockAsync(connection, ct))
            {
                logger.LogInformation("Migration lock is held by another instance, waiting...");

                await Task.Delay(RetryDelay, ct);
            }

            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

                if (pending.Count == 0)
                {
                    logger.LogInformation("Database {Database} is up to date", connection.Database);
                    return;
                }

                logger.LogInformation("Applying {Count} migration(s) to {Database}: {Migrations}",
                    pending.Count, connection.Database, string.Join(", ", pending));

                await db.Database.MigrateAsync(ct);

                logger.LogInformation("Migrations applied to {Database}", connection.Database);
            }
            finally
            {
                await ExecuteScalarAsync(connection, "SELECT pg_advisory_unlock(@key)", CancellationToken.None);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> TryAcquireLockAsync(DbConnection connection, CancellationToken ct)
    {
        var result = await ExecuteScalarAsync(connection, "SELECT pg_try_advisory_lock(@key)", ct);

        return result is true;
    }

    private static async Task<object?> ExecuteScalarAsync(DbConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = MigrationLockKey;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync(ct);
    }
}
