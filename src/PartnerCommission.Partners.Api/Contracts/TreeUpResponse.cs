namespace PartnerCommission.Partners.Api.Contracts;

public sealed record TreeUpResponse(
    string Root, IReadOnlyList<string> Chain
    );
