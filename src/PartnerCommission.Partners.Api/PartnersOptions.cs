using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Partners.Api;

public class PartnersOptions
{
    public const string Section = "Partners";

    [Range(1, 25)]
    public int MaxDepth { get; init; } = 10;
}
