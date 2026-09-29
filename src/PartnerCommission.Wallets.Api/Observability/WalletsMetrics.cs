using Prometheus;

namespace PartnerCommission.Wallets.Api.Observability;

internal static class WalletsMetrics
{
    private const string Prefix = "partnercommission_";

    public static readonly Counter CommissionsReceived = Metrics.CreateCounter(
        Prefix + "commissions_received_total",
        "Accrued commissions received from Commissions",
        "result");

    public static readonly Counter Payouts = Metrics.CreateCounter(
        Prefix + "payouts_total",
        "Commissions paid out to wallets");

    public static readonly Counter PayoutAmount = Metrics.CreateCounter(
        Prefix + "payout_amount_total",
        "Total amount paid out to wallets");

    public static readonly Histogram PayoutRunSeconds = Metrics.CreateHistogram(
        Prefix + "payout_run_seconds",
        "Duration of a payout run",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(start: 0.01, factor: 2, count: 12) });

    public static readonly Gauge WalletEntriesPending = Metrics.CreateGauge(
        Prefix + "wallet_entries_pending",
        "Commissions received but not paid out yet");
}
