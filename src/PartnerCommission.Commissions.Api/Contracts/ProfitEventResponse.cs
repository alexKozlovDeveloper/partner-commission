using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Contracts;

public sealed record ProfitEventResponse(
    string EventExternalId,
    decimal Profit,
    SchemaType SchemaType,
    ProfitEventStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc
    );
