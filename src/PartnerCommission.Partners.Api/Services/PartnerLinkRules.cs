namespace PartnerCommission.Partners.Api.Services;

public enum PartnerLinkViolation
{
    None = 0,
    SelfReference = 1,
    Cycle = 2,
    DepthExceeded = 3
}

public static class PartnerLinkRules
{
    public static PartnerLinkViolation Check(
        Guid userId,
        Guid partnerId,
        IReadOnlyCollection<Guid> partnerAncestorIds,
        int userSubtreeHeight,
        int maxDepth
        )
    {
        if (userId == partnerId)
            return PartnerLinkViolation.SelfReference;

        if (partnerAncestorIds.Contains(userId))
            return PartnerLinkViolation.Cycle;

        var deepestAncestorsCount = userSubtreeHeight + 1 + partnerAncestorIds.Count;

        if (deepestAncestorsCount > maxDepth)
            return PartnerLinkViolation.DepthExceeded;

        return PartnerLinkViolation.None;
    }
}
