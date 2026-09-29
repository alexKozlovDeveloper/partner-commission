namespace PartnerCommission.Commissions.Domain;

public interface ICommissionCalculator
{
    IReadOnlyList<BeneficiaryLineWithCommission> Calculate(decimal profit, SchemaType schemaType, IReadOnlyList<BeneficiaryLine> beneficiaries);
}
