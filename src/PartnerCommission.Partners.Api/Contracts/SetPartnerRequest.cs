using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Partners.Api.Contracts;

public sealed record SetPartnerRequest(
    [property: Required, MaxLength(64)] string PartnerExternalId
    );
