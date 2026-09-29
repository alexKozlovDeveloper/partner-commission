namespace PartnerCommission.Commissions.Domain;

public class FibonacciSchema : ICommissionSchema
{
    private const int MaxLevel = 92;

    private static readonly long[] Sequence = Build();

    public SchemaType Type => SchemaType.Fibonacci;

    public decimal RateFor(int level)
    {
        if (level < 0)
            return 0;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, MaxLevel);

        return Sequence[level];
    }

    private static long[] Build()
    {
        var sequence = new long[MaxLevel + 1];

        sequence[0] = 0;
        sequence[1] = 1;

        for (var i = 2; i <= MaxLevel; i++)
            sequence[i] = sequence[i - 1] + sequence[i - 2];

        return sequence;
    }
}
