namespace PartnerCommission.Commissions.Domain;

internal interface ICommissionSchema
{
    SchemaType Type { get; }
    decimal RateFor(int level);
}
