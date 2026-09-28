namespace PartnerCommission.Commissions.Domain.Tests;

public class LinearSchemaTests
{
    private readonly LinearSchema _schema = new();

    [Fact]
    public void Type_IsLinear()
    {
        Assert.Equal(SchemaType.Linear, _schema.Type);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void RateFor_ReturnsLevel(int level)
    {
        var rate = _schema.RateFor(level);

        Assert.Equal(level, rate);
    }
}
