using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Data;

public static class CommissionsDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<CommissionsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommissionsDbContext>>();

        await InsertSettingIfMissingAsync(db, logger, SettingKeys.CommissionSchema, nameof(SchemaType.Linear), ct);
    }

    private static async Task InsertSettingIfMissingAsync(
        CommissionsDbContext db, ILogger logger, string key, string value, CancellationToken ct)
    {
        var inserted = await db.Database.ExecuteSqlAsync($"""
            INSERT INTO settings ("Key", "Value")
            VALUES ({key}, {value})
            ON CONFLICT ("Key") DO NOTHING
            """, ct);

        if (inserted > 0)
            logger.LogInformation("Setting {Key} initialized with default value {Value}", key, value);
    }
}
