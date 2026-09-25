using Microsoft.EntityFrameworkCore;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Data;
using PartnerCommission.Partners.Api.Entities;

namespace PartnerCommission.Partners.Api.Services;

public class UsersService(
    PartnersDbContext dbContext
    ) : IUserService
{
    public async Task<Guid> CreateAsync(CreateUserRequest createUserModel, CancellationToken ct)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = createUserModel.ExternalId,
            ParentId = null,
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(ct);

        return user.Id;
    }

    public async Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct)
    {
        var partnerExternalId = setPartnerModel.PartnerExternalId;

        var users = await dbContext.Users
            .Where(x => x.ExternalId == externalId || x.ExternalId == setPartnerModel.PartnerExternalId)
            .ToListAsync(ct);

        var user = users
            .Where(x => x.ExternalId == externalId)
            .FirstOrDefault() ?? throw new NotImplementedException();

        var partner = users
            .Where(x => x.ExternalId == partnerExternalId)
            .FirstOrDefault() ?? throw new NotImplementedException();

        user.Parent = partner;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<TreeUpResponse> GetTreeUpAsync(string externalId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<TreeDownResponse> GetTreeDownAsync(string externalId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
