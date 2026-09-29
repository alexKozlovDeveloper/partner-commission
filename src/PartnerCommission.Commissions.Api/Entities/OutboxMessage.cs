namespace PartnerCommission.Commissions.Api.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
}
