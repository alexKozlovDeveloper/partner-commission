using PartnerCommission.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

public interface IPartnersClient
{
    Task<AncestorsResponse?> GetAncestorsAsync(string userExternalId, CancellationToken ct);
}
