namespace PartnerCommission.Commissions.Domain.Tests;

public class FibonacciSchemaTests
{
    private readonly FibonacciSchema _schema = new();

    public static TheoryData<int, decimal> Rates => new()
    {
        { 1, 1m },
        { 2, 1m },
        { 3, 2m },
        { 4, 3m },
        { 5, 5m },
        { 6, 8m },
        { 10, 55m },
    };

    [Fact]
    public void Type_IsFibonacci()
    {
        Assert.Equal(SchemaType.Fibonacci, _schema.Type);
    }

    [Theory]
    [MemberData(nameof(Rates))]
    public void RateFor_ReturnsFibonacciNumber(int level, decimal expected)
    {
        var rate = _schema.RateFor(level);

        Assert.Equal(expected, rate);
    }

    [Fact]
    public void RateFor_LargeLevel_DoesNotOverflow()
    {
        var rate = _schema.RateFor(50);

        Assert.Equal(12_586_269_025m, rate);
    }

    [Fact]
    public void RateFor_LargerLevelFirst_SmallerLevelsStillCorrect()
    {
        Assert.Equal(55m, _schema.RateFor(10));

        Assert.Equal(1m, _schema.RateFor(1));
        Assert.Equal(1m, _schema.RateFor(2));
        Assert.Equal(2m, _schema.RateFor(3));
        Assert.Equal(21m, _schema.RateFor(8));
    }

    [Fact]
    public void RateFor_SmallerLevelFirst_LargerLevelsStillCorrect()
    {
        Assert.Equal(2m, _schema.RateFor(3));

        Assert.Equal(21m, _schema.RateFor(8));
        Assert.Equal(55m, _schema.RateFor(10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RateFor_NonPositiveLevel_ReturnsZero(int level)
    {
        var rate = _schema.RateFor(level);

        Assert.Equal(0m, rate);
    }
}
