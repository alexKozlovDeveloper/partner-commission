namespace PartnerCommission.Commissions.Domain;

public class LinearSchema : ICommissionSchema
{
    public SchemaType Type => SchemaType.Linear;

    public decimal RateFor(int level) => level;
}
