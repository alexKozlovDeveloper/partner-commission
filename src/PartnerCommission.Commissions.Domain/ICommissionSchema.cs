namespace PartnerCommission.Commissions.Domain;

public interface ICommissionSchema
{
    SchemaType Type { get; }
    decimal RateFor(int level);
}
