namespace PartnerCommission.Wallets.Api.Entities;

public class WalletEntry
{
    public Guid CommissionId { get; set; }
    public required string UserExternalId { get; set; }
    public required string EventExternalId { get; set; }
    public decimal Amount { get; set; }
    public WalletEntryStatus Status { get; set; }
    public DateTime AccruedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
