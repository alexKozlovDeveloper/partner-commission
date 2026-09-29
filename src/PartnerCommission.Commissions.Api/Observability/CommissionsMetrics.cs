using Prometheus;

namespace PartnerCommission.Commissions.Api.Observability;

internal static class CommissionsMetrics
{
    private const string Prefix = "partnercommission_";

    public static readonly Counter ProfitEventsReceived = Metrics.CreateCounter(
        Prefix + "profit_events_received_total",
        "Profit events received by the API",
        "result");

    public static readonly Counter ProfitEventsProcessed = Metrics.CreateCounter(
        Prefix + "profit_events_processed_total",
        "Profit event processing attempts by outcome",
        "outcome");

    public static readonly Histogram ProfitEventProcessingSeconds = Metrics.CreateHistogram(
        Prefix + "profit_event_processing_seconds",
        "Duration of processing a single profit event (including the call to Partners)",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(start: 0.005, factor: 2, count: 12) });

    public static readonly Counter CommissionsAccrued = Metrics.CreateCounter(
        Prefix + "commissions_accrued_total",
        "Commissions accrued",
        "schema");

    public static readonly Counter CommissionsAccruedAmount = Metrics.CreateCounter(
        Prefix + "commissions_accrued_amount_total",
        "Total amount of accrued commissions",
        "schema");

    public static readonly Counter OutboxMessagesSent = Metrics.CreateCounter(
        Prefix + "outbox_messages_sent_total",
        "Outbox messages delivery attempts by result",
        "result");

    public static readonly Gauge OutboxPending = Metrics.CreateGauge(
        Prefix + "outbox_pending",
        "Outbox messages waiting to be delivered");

    public static readonly Gauge ProfitEventsPending = Metrics.CreateGauge(
        Prefix + "profit_events_pending",
        "Profit events waiting to be processed (Received or Unresolved)");
}
