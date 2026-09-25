using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Commissions.Api.Contracts;

public sealed record CreateEventRequest(
    [Required, MaxLength(64)] string EventExternalId,
    decimal Profit);
