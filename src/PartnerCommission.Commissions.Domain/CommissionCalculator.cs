namespace PartnerCommission.Commissions.Domain;

public sealed record BeneficiaryLine(
    string BeneficiaryExternalId,
    int Level
    );

public sealed record BeneficiaryLineWithCommission(
    string BeneficiaryExternalId,
    int Level,
    decimal Amount
    );

public sealed class CommissionCalculator : ICommissionCalculator
{
    public const int AmountDecimals = 4;

    public IReadOnlyList<BeneficiaryLineWithCommission> Calculate(decimal profit, SchemaType schemaType, IReadOnlyList<BeneficiaryLine> beneficiaries)
    {
        ArgumentNullException.ThrowIfNull(beneficiaries);

        if (profit <= 0 || beneficiaries.Count == 0)
            return [];

        var schema = CommissionSchemas.For(schemaType);

        var lines = new List<BeneficiaryLineWithCommission>(beneficiaries.Count);

        foreach (var beneficiary in beneficiaries) 
        {
            var amount = Math.Round(
                schema.RateFor(beneficiary.Level) * profit / 100m,
                AmountDecimals,
                MidpointRounding.AwayFromZero
                );

            if (amount > 0)
            {
                lines.Add(new BeneficiaryLineWithCommission(beneficiary.BeneficiaryExternalId, beneficiary.Level, amount));
            }
        }

        return lines;
    }
}
