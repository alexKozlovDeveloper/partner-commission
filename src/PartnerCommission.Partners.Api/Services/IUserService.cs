using PartnerCommission.Partners.Api.Contracts;

namespace PartnerCommission.Partners.Api.Services;

public interface IUserService
{
    Task<Guid> CreateAsync(CreateUserRequest createUserModel, CancellationToken ct);
    Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct);
    Task<TreeUpResponse> GetTreeUpAsync(string externalId, CancellationToken ct);
    Task<TreeDownResponse> GetTreeDownAsync(string externalId, CancellationToken ct);
}
