namespace PartnerCommission.Partners.Api.Services;

public sealed record CreateUserResult(
    Guid Id,
    bool Duplicate
    );
