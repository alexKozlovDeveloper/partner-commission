namespace PartnerCommission.Commissions.Domain.Tests;

public class CommissionCalculatorTests
{
    private readonly CommissionCalculator _calculator = new();

    private static readonly IReadOnlyList<BeneficiaryLine> ThreeLevels =
    [
        new("u1", 1),
        new("u2", 2),
        new("u3", 3),
    ];

    private static IReadOnlyList<BeneficiaryLine> Levels(int count) =>
        Enumerable
            .Range(1, count)
            .Select(level => new BeneficiaryLine($"u{level}", level))
            .ToList();

    public static TheoryData<decimal> NonPositiveProfits => new() { 0m, -0.0001m, -100m };

    [Fact]
    public void Calculate_Linear_ChargesLevelPercentOnEachLevel()
    {
        var lines = _calculator.Calculate(100m, SchemaType.Linear, ThreeLevels);

        Assert.Equal(
            [
                new BeneficiaryLineWithCommission("u1", 1, 1m),
                new BeneficiaryLineWithCommission("u2", 2, 2m),
                new BeneficiaryLineWithCommission("u3", 3, 3m),
            ],
            lines
            );
    }

    [Fact]
    public void Calculate_Fibonacci_ChargesFibonacciPercentOnEachLevel()
    {
        var lines = _calculator.Calculate(100m, SchemaType.Fibonacci, ThreeLevels);

        Assert.Equal(
            [
                new BeneficiaryLineWithCommission("u1", 1, 1m),
                new BeneficiaryLineWithCommission("u2", 2, 1m),
                new BeneficiaryLineWithCommission("u3", 3, 2m),
            ],
            lines
            );
    }

    [Theory]
    [InlineData(SchemaType.Linear, 55)]
    [InlineData(SchemaType.Fibonacci, 143)]
    public void Calculate_TenLevels_TotalMatchesSchema(SchemaType schemaType, int expectedTotal)
    {
        var total = _calculator
            .Calculate(100m, schemaType, Levels(10))
            .Sum(x => x.Amount);

        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [MemberData(nameof(NonPositiveProfits))]
    public void Calculate_NonPositiveProfit_ReturnsEmpty(decimal profit)
    {
        Assert.Empty(_calculator.Calculate(profit, SchemaType.Linear, ThreeLevels));
        Assert.Empty(_calculator.Calculate(profit, SchemaType.Fibonacci, ThreeLevels));
    }

    [Fact]
    public void Calculate_NoBeneficiaries_ReturnsEmpty()
    {
        Assert.Empty(_calculator.Calculate(100m, SchemaType.Linear, []));
    }

    [Fact]
    public void Calculate_NullBeneficiaries_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Calculate(100m, SchemaType.Linear, null!));
    }

    [Fact]
    public void Calculate_UsesLevelFromInput_NotPositionInList()
    {
        IReadOnlyList<BeneficiaryLine> shuffled = [new("u3", 3), new("u1", 1)];

        var lines = _calculator.Calculate(100m, SchemaType.Linear, shuffled);

        Assert.Equal(
            [
                new BeneficiaryLineWithCommission("u3", 3, 3m),
                new BeneficiaryLineWithCommission("u1", 1, 1m),
            ],
            lines
            );
    }

    [Fact]
    public void Calculate_RoundsToFourDecimals()
    {
        var line = Assert.Single(_calculator.Calculate(0.12345m, SchemaType.Linear, [new("u1", 1)]));

        Assert.Equal(0.0012m, line.Amount);
    }

    [Fact]
    public void Calculate_MidpointRoundsAwayFromZero()
    {
        var line = Assert.Single(_calculator.Calculate(0.005m, SchemaType.Linear, [new("u1", 1)]));

        Assert.Equal(0.0001m, line.Amount);
    }

    [Fact]
    public void Calculate_AmountRoundedToZero_IsSkipped()
    {
        Assert.Empty(_calculator.Calculate(0.001m, SchemaType.Linear, [new("u1", 1)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_NonPositiveLevel_IsSkipped(int level)
    {
        Assert.Empty(_calculator.Calculate(100m, SchemaType.Linear, [new("u1", level)]));
        Assert.Empty(_calculator.Calculate(100m, SchemaType.Fibonacci, [new("u1", level)]));
    }

    [Fact]
    public void Calculate_UnknownSchemaType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(100m, (SchemaType)99, ThreeLevels));
    }
}
