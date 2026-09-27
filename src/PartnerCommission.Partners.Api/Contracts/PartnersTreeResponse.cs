namespace PartnerCommission.Partners.Api.Contracts;

public sealed record PartnersTreeResponse(
    string ExternalId,
    IReadOnlyList<AncestorDto> Ancestors,
    IReadOnlyList<TreeNodeDto> Descendants
    );

public sealed record AncestorDto(
    string ExternalId, 
    int Level
    );

public sealed record TreeNodeDto(
    string ExternalId, 
    int Level, IReadOnlyList<TreeNodeDto> Children
    );