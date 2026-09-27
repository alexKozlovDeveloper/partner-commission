using PartnerCommission.Contracts;
using PartnerCommission.Partners.Api.Contracts;

namespace PartnerCommission.Partners.Api.Services;

public interface IUserService
{
    Task<Guid> CreateAsync(CreateUserRequest createUserModel, CancellationToken ct);
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct);

    Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct);
    Task DeletePartnerAsync(string externalId, CancellationToken ct);

    Task<PartnersTreeResponse> GetPartnersTreeAsync(string externalId, CancellationToken ct);

    //internal
    Task<AncestorsResponse> GetAncestorsAsync(string externalId, CancellationToken ct);
}
