using Prometheus;

namespace PartnerCommission.Partners.Api.Observability;

internal static class PartnersMetrics
{
    private const string Prefix = "partnercommission_";

    public static readonly Counter UsersCreated = Metrics.CreateCounter(
        Prefix + "users_created_total",
        "User creation requests by result",
        "result");

    public static readonly Counter PartnerLinks = Metrics.CreateCounter(
        Prefix + "partner_links_total",
        "Partner link requests by result",
        "result");
}
