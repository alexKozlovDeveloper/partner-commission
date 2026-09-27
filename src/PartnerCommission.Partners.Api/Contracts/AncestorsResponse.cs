namespace PartnerCommission.Partners.Api.Contracts;

public sealed record AncestorsResponse(
    string ExternalId,
    IReadOnlyList<AncestorItem> Ancestors
    );

public sealed record AncestorItem(
    string ExternalId,
    int Level
    );