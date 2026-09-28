using PartnerCommission.Partners.Api.Services;

namespace PartnerCommission.Partners.Api.Tests;

public class PartnerLinkRulesTests
{
    private const int MaxDepth = 10;

    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Partner = Guid.NewGuid();

    private static IReadOnlyCollection<Guid> Ancestors(int count)
    {
        var result = Enumerable
            .Range(0, count)
            .Select(_ => Guid.NewGuid())
            .ToList();

        return result;
    }

    [Fact]
    public void Check_RootPartnerAndNoDescendants_IsAllowed()
    {
        var violation = PartnerLinkRules.Check(User, Partner, [], userSubtreeHeight: 0, MaxDepth);

        Assert.Equal(PartnerLinkViolation.None, violation);
    }

    [Fact]
    public void Check_UserAsOwnPartner_IsSelfReference()
    {
        var violation = PartnerLinkRules.Check(User, User, [], userSubtreeHeight: 0, MaxDepth);

        Assert.Equal(PartnerLinkViolation.SelfReference, violation);
    }

    [Fact]
    public void Check_PartnerIsDirectChildOfUser_IsCycle()
    {
        var violation = PartnerLinkRules.Check(User, Partner, [User], userSubtreeHeight: 1, MaxDepth);

        Assert.Equal(PartnerLinkViolation.Cycle, violation);
    }

    [Fact]
    public void Check_PartnerIsDeepDescendantOfUser_IsCycle()
    {
        IReadOnlyCollection<Guid> partnerAncestors = [Guid.NewGuid(), Guid.NewGuid(), User];

        var violation = PartnerLinkRules.Check(User, Partner, partnerAncestors, userSubtreeHeight: 3, MaxDepth);

        Assert.Equal(PartnerLinkViolation.Cycle, violation);
    }

    [Fact]
    public void Check_CycleAndDepthBothViolated_ReportsCycle()
    {
        var partnerAncestors = Ancestors(MaxDepth).Append(User).ToList();

        var violation = PartnerLinkRules.Check(User, Partner, partnerAncestors, userSubtreeHeight: 5, MaxDepth);

        Assert.Equal(PartnerLinkViolation.Cycle, violation);
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(0, 9)]
    [InlineData(4, 5)]
    public void Check_ResultingDepthEqualsMax_IsAllowed(int partnerAncestorsCount, int userSubtreeHeight)
    {
        var violation = PartnerLinkRules.Check(User, Partner, Ancestors(partnerAncestorsCount), userSubtreeHeight, MaxDepth);

        Assert.Equal(PartnerLinkViolation.None, violation);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    [InlineData(5, 5)]
    public void Check_ResultingDepthAboveMax_IsDepthExceeded(int partnerAncestorsCount, int userSubtreeHeight)
    {
        var violation = PartnerLinkRules.Check(User, Partner, Ancestors(partnerAncestorsCount), userSubtreeHeight, MaxDepth);

        Assert.Equal(PartnerLinkViolation.DepthExceeded, violation);
    }

    [Fact]
    public void Check_PartnerChainAlreadyLongerThanMax_IsDepthExceeded()
    {
        var violation = PartnerLinkRules.Check(User, Partner, Ancestors(MaxDepth + 1), userSubtreeHeight: 0, MaxDepth);

        Assert.Equal(PartnerLinkViolation.DepthExceeded, violation);
    }
}
