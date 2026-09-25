using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Partners.Api.Contracts;

public sealed record SetPartnerRequest(
    [Required, MaxLength(64)] string PartnerExternalId
    );
