namespace PartnerCommission.Commissions.Api.Entities;

public class Setting
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public static class SettingKeys
{
    public const string CommissionSchema = "commission_schema";
}