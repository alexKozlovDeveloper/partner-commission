using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace PartnerCommission.Shared.Data;

public sealed class AdvisoryLock : IAsyncDisposable
{
    private readonly DbContext _db;
    private readonly long _key;

    private AdvisoryLock(DbContext db, long key)
    {
        _db = db;
        _key = key;
    }

    public static async Task<AdvisoryLock?> TryAcquireAsync(DbContext db, long key, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);

        try
        {
            var acquired = await ExecuteScalarAsync(db.Database.GetDbConnection(), "SELECT pg_try_advisory_lock(@key)", key, ct);

            if (acquired is true)
                return new AdvisoryLock(db, key);
        }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }

        await db.Database.CloseConnectionAsync();

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await ExecuteScalarAsync(_db.Database.GetDbConnection(), "SELECT pg_advisory_unlock(@key)", _key, CancellationToken.None);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<object?> ExecuteScalarAsync(DbConnection connection, string sql, long key, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = key;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync(ct);
    }
}
