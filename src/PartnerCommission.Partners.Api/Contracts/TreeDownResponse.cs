namespace PartnerCommission.Partners.Api.Contracts;

public sealed record TreeDownResponse(
    string Root, IReadOnlyList<IReadOnlyList<string>> Levels
    );
