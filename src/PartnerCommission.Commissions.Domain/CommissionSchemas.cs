namespace PartnerCommission.Commissions.Domain;

public static class CommissionSchemas
{
    private static readonly ICommissionSchema Linear = new LinearSchema();
    private static readonly ICommissionSchema Fibonacci = new FibonacciSchema();

    public static ICommissionSchema For(SchemaType type) => type switch
    {
        SchemaType.Linear => Linear,
        SchemaType.Fibonacci => Fibonacci,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown commission schema")
    };
}
