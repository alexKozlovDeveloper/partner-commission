namespace PartnerCommission.Commissions.Domain;

internal class FibonacciSchema : ICommissionSchema
{
    public SchemaType Type => SchemaType.Fibonacci;

    public decimal RateFor(int level)
    {
        throw new NotImplementedException();
    }
}
