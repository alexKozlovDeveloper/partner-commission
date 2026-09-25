namespace PartnerCommission.Commissions.Api.Entities;

public class ProfitEvent
{
    public Guid Id { get; set; }
    public required string EventExternalId { get; set; }
    public required string UserExternalId { get; set; }
    public decimal Profit { get; set; }
    public required string SchemaType { get; set; }
    public ProfitEventStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
