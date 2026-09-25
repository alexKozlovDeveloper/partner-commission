namespace PartnerCommission.Commissions.Domain;

internal class LinearSchema : ICommissionSchema
{
    public SchemaType Type => SchemaType.Linear;

    public decimal RateFor(int level) => level;
}
