using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using PartnerCommission.Contracts;
using PartnerCommission.Partners.Api.Contracts;
using PartnerCommission.Partners.Api.Data;
using PartnerCommission.Partners.Api.Entities;
using PartnerCommission.Partners.Api.Observability;
using PartnerCommission.Shared.Exceptions;
using PartnerCommission.Shared.Pagination;
using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Partners.Api.Services;

internal sealed class UsersService(
    PartnersDbContext dbContext,
    UserTreeQueries treeQueries,
    IOptions<PartnersOptions> partnersOptions
    ) : IUserService
{
    private const long TreeWriteLockKey = 42;

    public async Task<UserResponse> GetAsync(string externalId, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Where(x => x.ExternalId == externalId)
            .Select(x => new UserResponse(
                x.Id,
                x.ExternalId,
                x.ParentId,
                x.Parent != null ? x.Parent.ExternalId : null
                ))
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(User), externalId);

        return user;
    }

    public async Task<CreateUserResult> CreateAsync(CreateUserRequest createUserModel, CancellationToken ct)
    {
        var existingId = await FindUserIdAsync(createUserModel.ExternalId, ct);

        if (existingId is not null)
        {
            PartnersMetrics.UsersCreated.WithLabels("duplicate").Inc();

            return new CreateUserResult(existingId.Value, Duplicate: true);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = createUserModel.ExternalId,
            ParentId = null,
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();

            var concurrentId = await FindUserIdAsync(createUserModel.ExternalId, ct)
                ?? throw new InvalidOperationException($"User '{createUserModel.ExternalId}' violated unique index but was not found", ex);

            PartnersMetrics.UsersCreated.WithLabels("duplicate").Inc();

            return new CreateUserResult(concurrentId, Duplicate: true);
        }

        PartnersMetrics.UsersCreated.WithLabels("created").Inc();

        return new CreateUserResult(user.Id, Duplicate: false);
    }

    private async Task<Guid?> FindUserIdAsync(string externalId, CancellationToken ct)
    {
        var result = await dbContext.Users
            .Where(x => x.ExternalId == externalId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        return result;
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(PageRequest page, CancellationToken ct)
    {
        var users = await dbContext.Users
            .OrderBy(x => x.ExternalId)
            .Select(x => new UserResponse(
                x.Id,
                x.ExternalId,
                x.ParentId,
                x.Parent != null ? x.Parent.ExternalId : null
                ))
            .ToPagedAsync(page, ct);

        return users;
    }

    public async Task SetPartnerAsync(string externalId, SetPartnerRequest setPartnerModel, CancellationToken ct)
    {
        var partnerExternalId = setPartnerModel.PartnerExternalId;

        if (externalId == partnerExternalId)
        {
            PartnersMetrics.PartnerLinks.WithLabels("self").Inc();

            throw new ValidationException("User cannot be their own partner");
        }

        var maxDepth = partnersOptions.Value.MaxDepth;

        var strategy = dbContext.Database.CreateExecutionStrategy();

        var result = "linked";

        await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            await dbContext.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({TreeWriteLockKey})", ct);

            var users = await dbContext.Users
                .Where(x => x.ExternalId == externalId || x.ExternalId == partnerExternalId)
                .ToListAsync(ct);

            var user = users.FirstOrDefault(x => x.ExternalId == externalId)
                ?? throw new NotFoundException(nameof(User), externalId);

            var partner = users.FirstOrDefault(x => x.ExternalId == partnerExternalId)
                ?? throw new NotFoundException(nameof(User), partnerExternalId);

            if (user.ParentId == partner.Id)
            {
                result = "unchanged";
                return;
            }

            var partnerAncestors = await treeQueries.GetAncestorsAsync(partner.Id, maxDepth + 1, ct);
            var userSubtreeHeight = await treeQueries.GetSubtreeHeightAsync(user.Id, maxDepth + 1, ct);

            var partnerAncestorIds = partnerAncestors
                .Select(x => x.Id)
                .ToList();

            var violation = PartnerLinkRules.Check(
                user.Id,
                partner.Id,
                partnerAncestorIds,
                userSubtreeHeight,
                maxDepth
                );

            switch (violation)
            {
                case PartnerLinkViolation.None:
                    break;

                case PartnerLinkViolation.SelfReference:
                    PartnersMetrics.PartnerLinks.WithLabels("self").Inc();
                    throw new ValidationException("User cannot be their own partner");

                case PartnerLinkViolation.Cycle:
                    PartnersMetrics.PartnerLinks.WithLabels("cycle").Inc();
                    throw new ConflictException($"User '{partnerExternalId}' is a descendant of '{externalId}': the link would create a cycle");

                case PartnerLinkViolation.DepthExceeded:
                    PartnersMetrics.PartnerLinks.WithLabels("depth_exceeded").Inc();
                    throw new ConflictException($"Linking '{externalId}' to '{partnerExternalId}' exceeds max tree depth of {maxDepth}");

                default:
                    throw new InvalidOperationException($"Unknown partner link violation '{violation}'");
            }

            user.ParentId = partner.Id;

            await dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        });

        PartnersMetrics.PartnerLinks.WithLabels(result).Inc();
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
        var maxDepth = partnersOptions.Value.MaxDepth;

        var userId = await FindUserIdAsync(externalId, ct)
            ?? throw new NotFoundException(nameof(User), externalId);

        var ancestors = await treeQueries.GetAncestorsAsync(userId, maxDepth, ct);
        var descendants = await treeQueries.GetDescendantsAsync(userId, maxDepth, ct);

        var childrenByParent = descendants.ToLookup(x => x.ParentId);

        IReadOnlyList<TreeNodeDto> BuildChildren(Guid parentId) => childrenByParent[parentId]
            .Select(x => new TreeNodeDto(x.ExternalId, x.Level, BuildChildren(x.Id)))
            .ToList();

        var result = new PartnersTreeResponse(
            externalId,
            ancestors.Select(x => new AncestorDto(x.ExternalId, x.Level)).ToList(),
            BuildChildren(userId));

        return result;
    }

    public async Task<AncestorsResponse> GetAncestorsAsync(string externalId, CancellationToken ct)
    {
        var userId = await FindUserIdAsync(externalId, ct)
            ?? throw new NotFoundException(nameof(User), externalId);

        var ancestors = await treeQueries.GetAncestorsAsync(userId, partnersOptions.Value.MaxDepth, ct);

        var result = new AncestorsResponse(
            externalId,
            ancestors.Select(x => new AncestorItem(x.ExternalId, x.Level)).ToList());

        return result;
    }
}

