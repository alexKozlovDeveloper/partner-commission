using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Services;

public interface ICommissionSchemaSettings
{
    Task<SchemaType> GetCurrentAsync(CancellationToken ct);
    Task SetAsync(SchemaType schema, CancellationToken ct);
}
