using PartnerCommission.Commissions.Domain;

namespace PartnerCommission.Commissions.Api.Entities;

public class Commission
{
    public Guid Id { get; set; }
    public required string ProfitEventId { get; set; }
    public required string BeneficiaryExternalId { get; set; }
    public int Level { get; set; }
    public decimal Amount { get; set; }
    public SchemaType SchemaType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}