namespace PartnerCommission.Partners.Api.Contracts;

public sealed record CreateUserResponse(
    Guid Id,
    string ExternalId
    );
