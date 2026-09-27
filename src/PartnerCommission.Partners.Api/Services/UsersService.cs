using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PartnerCommission.Contracts;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Data;
using PartnerCommission.Partners.Api.Entities;
using PartnerCommission.Shared.Exceptions;

namespace PartnerCommission.Partners.Api.Services;

public class UsersService(
    PartnersDbContext dbContext,
    IOptions<PartnersOptions> partnersOptions
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

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct) 
    {
        var users = await dbContext.Users
            .Select(x => new UserResponse(
                x.Id,
                x.ExternalId,
                x.ParentId,
                x.ParentId != null ? x.Parent.ExternalId : null
                ))
            .ToListAsync(ct);

        return users;
    }

    public async Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct)
    {
        var d = partnersOptions.Value;

        var partnerExternalId = setPartnerModel.PartnerExternalId;

        var users = await dbContext.Users
            .Where(x => x.ExternalId == externalId || x.ExternalId == setPartnerModel.PartnerExternalId)
            .ToListAsync(ct);

        var user = users
            .Where(x => x.ExternalId == externalId)
            .FirstOrDefault() ?? throw new NotFoundException(nameof(User), externalId);

        var partner = users
            .Where(x => x.ExternalId == partnerExternalId)
            .FirstOrDefault() ?? throw new NotFoundException(nameof(User), partnerExternalId);

        user.Parent = partner;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeletePartnerAsync(string externalId, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Where(x => x.ExternalId == externalId)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(User), externalId);

        user.ParentId = null;

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<PartnersTreeResponse> GetPartnersTreeAsync(string externalId, CancellationToken ct)
    {
        // TODO: stub, must be rework (bad optimization for now)
        
        var user = await dbContext.Users
            .Where(x => x.ExternalId == externalId)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(User), externalId);

        var ancestors = new List<AncestorDto>();

        {
            Guid? ancestorId = user.ParentId;
            var level = 1;
            while (ancestorId != null)
            {
                var ancestor = await dbContext.Users
                    .Where(x => x.Id == ancestorId)
                    .FirstAsync(ct);

                ancestors.Add(new AncestorDto(ancestor.ExternalId, level));

                ancestorId = ancestor.ParentId;
                level++;
            }
        }

        var descendants = await GetDescendantsRecursionAsync(level: 1, user.Id, ct);

        var result = new PartnersTreeResponse(externalId, ancestors, descendants);

        return result;
    }

    private async Task<List<TreeNodeDto>> GetDescendantsRecursionAsync(int level, Guid parentId, CancellationToken ct)
    {
        var descendants = new List<TreeNodeDto>();

        var childs = await dbContext.Users
            .Where(x => x.ParentId == parentId)
            .ToListAsync(ct);

        foreach (var child in childs)
        {
            var subChilds = await GetDescendantsRecursionAsync(level + 1, child.Id, ct);
            descendants.Add(new TreeNodeDto(child.ExternalId, level, subChilds));
        }

        return descendants;
    }

    public async Task<AncestorsResponse> GetAncestorsAsync(string externalId, CancellationToken ct) 
    {
        // TODO: stub, must be rework (bad optimization for now)

        var user = await dbContext.Users
            .Where(x => x.ExternalId == externalId)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(User), externalId);

        var ancestors = new List<AncestorItem>();
                
        Guid? ancestorId = user.ParentId;
        var level = 1;
        while (ancestorId != null)
        {
            var ancestor = await dbContext.Users
                .Where(x => x.Id == ancestorId)
                .FirstAsync(ct);

            ancestors.Add(new AncestorItem(ancestor.ExternalId, level));

            ancestorId = ancestor.ParentId;
            level++;
        }

        var result = new AncestorsResponse(externalId, ancestors);

        return result;
    }
}
