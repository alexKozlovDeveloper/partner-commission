namespace PartnerCommission.Wallets.Api.Entities;

public class Wallet
{
    public required string UserExternalId { get; set; }
    public decimal Balance { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
