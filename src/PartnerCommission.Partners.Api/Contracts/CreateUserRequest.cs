using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Partners.Api.Contracts;

public sealed record CreateUserRequest(
    [property: Required, MaxLength(64)] string ExternalId
    );