using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Contracts;

public sealed record SetSchemaRequest(
    SchemaType SchemaType
    );
