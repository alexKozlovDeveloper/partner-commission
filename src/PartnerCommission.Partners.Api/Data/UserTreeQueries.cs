using Microsoft.EntityFrameworkCore;

namespace PartnerCommission.Partners.Api.Data;

public sealed class TreeRow
{
    public Guid Id { get; init; }
    public required string ExternalId { get; init; }
    public Guid? ParentId { get; init; }
    public int Level { get; init; }
}

public sealed class UserTreeQueries(PartnersDbContext dbContext)
{
    public Task<List<TreeRow>> GetAncestorsAsync(Guid userId, int maxDepth, CancellationToken ct)
    {
        var result = dbContext.Database.SqlQuery<TreeRow>($"""
            WITH RECURSIVE up AS (
                SELECT p."Id", p."ExternalId", p."ParentId", 1 AS "Level"
                FROM users u
                JOIN users p ON p."Id" = u."ParentId"
                WHERE u."Id" = {userId}

                UNION ALL

                SELECT p."Id", p."ExternalId", p."ParentId", up."Level" + 1
                FROM up
                JOIN users p ON p."Id" = up."ParentId"
                WHERE up."Level" < {maxDepth}
            )
            SELECT "Id", "ExternalId", "ParentId", "Level"
            FROM up
            ORDER BY "Level"
            """)
            .ToListAsync(ct);

        return result;
    }

    public Task<List<TreeRow>> GetDescendantsAsync(Guid userId, int maxDepth, CancellationToken ct)
    {
        var result = dbContext.Database.SqlQuery<TreeRow>($"""
            WITH RECURSIVE down AS (
                SELECT c."Id", c."ExternalId", c."ParentId", 1 AS "Level"
                FROM users c
                WHERE c."ParentId" = {userId}

                UNION ALL

                SELECT c."Id", c."ExternalId", c."ParentId", d."Level" + 1
                FROM down d
                JOIN users c ON c."ParentId" = d."Id"
                WHERE d."Level" < {maxDepth}
            )
            SELECT "Id", "ExternalId", "ParentId", "Level"
            FROM down
            ORDER BY "Level", "ExternalId"
            """)
         .ToListAsync(ct);

        return result;
    }

    public async Task<int> GetSubtreeHeightAsync(Guid userId, int maxDepth, CancellationToken ct)
    {
        var result = await dbContext.Database.SqlQuery<int>($"""
            WITH RECURSIVE down AS (
                SELECT c."Id", 1 AS "Level"
                FROM users c
                WHERE c."ParentId" = {userId}

                UNION ALL

                SELECT c."Id", d."Level" + 1
                FROM down d
                JOIN users c ON c."ParentId" = d."Id"
                WHERE d."Level" < {maxDepth}
            )
            SELECT COALESCE(MAX("Level"), 0) AS "Value"
            FROM down
            """)
            .ToListAsync(ct);

        return result[0];
    }
}
