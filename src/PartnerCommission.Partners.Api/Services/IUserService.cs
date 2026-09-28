using PartnerCommission.Contracts;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Shared.Pagination;

namespace PartnerCommission.Partners.Api.Services;

public interface IUserService
{
    Task<UserResponse> GetAsync(string externalId, CancellationToken ct);
    Task<CreateUserResult> CreateAsync(CreateUserRequest createUserModel, CancellationToken ct);
    Task<PagedResponse<UserResponse>> ListAsync(PageRequest page, CancellationToken ct);

    Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct);
    Task DeletePartnerAsync(string externalId, CancellationToken ct);

    Task<PartnersTreeResponse> GetPartnersTreeAsync(string externalId, CancellationToken ct);

    // internal
    Task<AncestorsResponse> GetAncestorsAsync(string externalId, CancellationToken ct);
}
