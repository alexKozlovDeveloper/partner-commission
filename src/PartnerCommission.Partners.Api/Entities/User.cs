namespace PartnerCommission.Partners.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; }
    public Guid? ParentId { get; set; }
    public User? Parent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}