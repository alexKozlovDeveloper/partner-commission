using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PartnerCommission.Shared.Data;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex)
    {
        var result = ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        return result;
    }
}
