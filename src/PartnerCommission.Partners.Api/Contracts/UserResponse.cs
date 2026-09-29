namespace PartnerCommission.Partners.Api.Contracts;

public sealed record UserResponse(
    Guid Id,
    string ExternalId,
    Guid? ParentId,
    string? ParentExternalId
    );