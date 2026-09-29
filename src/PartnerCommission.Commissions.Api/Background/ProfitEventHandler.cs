using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Commissions.Api.Services;
using PartnerCommission.Commissions.Domain;
using PartnerCommission.Contracts;
using PartnerCommission.Shared.Diagnostics;
using Prometheus;
using System.Text.Json;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class ProfitEventHandler(
    CommissionsDbContext db,
    IPartnersClient partnersClient,
    ICommissionCalculator calculator,
    ILogger<ProfitEventHandler> logger)
{
    private static readonly TimeSpan UnresolvedRetryDelay = TimeSpan.FromSeconds(30);

    public async Task HandleAsync(Guid profitEventId, CancellationToken ct)
    {
        using var activity = Tracing.Source.StartActivity("ProcessProfitEvent");

        var profitEvent = await db.ProfitEvents
            .Where(x => x.Id == profitEventId)
            .SingleAsync(ct);

        activity?.SetTag("event.external_id", profitEvent.EventExternalId);

        using var _ = logger.BeginScope("EventExternalId: {EventExternalId}", profitEvent.EventExternalId);

        if (profitEvent.Status is not (ProfitEventStatus.Received or ProfitEventStatus.Unresolved))
        {
            logger.LogDebug("Event is already {Status}, skipping", profitEvent.Status);
            return;
        }

        using var timer = CommissionsMetrics.ProfitEventProcessingSeconds.NewTimer();

        try
        {
            await ProcessAsync(profitEvent, ct);

            CommissionsMetrics.ProfitEventsProcessed
                .WithLabels(profitEvent.Status == ProfitEventStatus.Unresolved ? "unresolved" : "processed")
                .Inc();

            logger.LogInformation("Event handled with status {Status}", profitEvent.Status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();

            await ScheduleRetryAsync(profitEvent.Id, profitEvent.Attempts + 1, ex, ct);

            CommissionsMetrics.ProfitEventsProcessed.WithLabels("retry").Inc();
        }
    }

    private async Task ProcessAsync(ProfitEvent profitEvent, CancellationToken ct)
    {
        IReadOnlyList<BeneficiaryLineWithCommission> lines = [];

        if (profitEvent.Profit > 0)
        {
            var ancestors = await partnersClient.GetAncestorsAsync(profitEvent.UserExternalId, ct);

            if (ancestors is not null)
            {
                var now = DateTime.UtcNow;

                var beneficiary = ancestors.Ancestors
                    .Select(x => new BeneficiaryLine(x.ExternalId, x.Level))
                    .ToList();

                lines = calculator.Calculate(profitEvent.Profit, profitEvent.SchemaType, beneficiary);

                foreach (var line in lines)
                {
                    var commission = new Commission
                    {
                        Id = Guid.NewGuid(),
                        ProfitEventId = profitEvent.Id,
                        BeneficiaryExternalId = line.BeneficiaryExternalId,
                        Level = line.Level,
                        Amount = line.Amount,
                        SchemaType = profitEvent.SchemaType,
                        CreatedAtUtc = now
                    };

                    db.Commissions.Add(commission);

                    var payload = new CommissionAccruedMessage(
                        commission.Id,
                        profitEvent.EventExternalId,
                        commission.BeneficiaryExternalId,
                        commission.Amount,
                        now);

                    var message = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        Type = CommissionAccruedMessage.MessageType,
                        Payload = JsonSerializer.Serialize(payload),
                        CreatedAtUtc = now,
                        Attempts = 0,
                        LastError = null,
                        NextAttemptAtUtc = now
                    };

                    db.OutboxMessages.Add(message);
                }

                profitEvent.Status = ProfitEventStatus.Processed;
            }
            else
            {
                profitEvent.Status = ProfitEventStatus.Unresolved;
                profitEvent.NextAttemptAtUtc = DateTime.UtcNow + UnresolvedRetryDelay;
            }
        }
        else
        {
            profitEvent.Status = ProfitEventStatus.Processed;
        }

        if (profitEvent.Status == ProfitEventStatus.Processed)
            profitEvent.ProcessedAtUtc = DateTime.UtcNow;

        profitEvent.LastError = null;

        await db.SaveChangesAsync(ct);

        if (lines.Count > 0)
        {
            var schema = profitEvent.SchemaType.ToString();

            CommissionsMetrics.CommissionsAccrued.WithLabels(schema).Inc(lines.Count);
            CommissionsMetrics.CommissionsAccruedAmount.WithLabels(schema).Inc((double)lines.Sum(x => x.Amount));
        }
    }

    private async Task ScheduleRetryAsync(Guid eventId, int attempts, Exception ex, CancellationToken ct)
    {
        var profitEvent = await db.ProfitEvents
             .Where(x => x.Id == eventId)
             .SingleAsync(ct);

        var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempts), 300));

        profitEvent.Attempts = attempts;
        profitEvent.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
        profitEvent.NextAttemptAtUtc = DateTime.UtcNow + delay;

        await db.SaveChangesAsync(ct);

        logger.LogWarning(ex, "Event failed (attempt {Attempts}), retry in {Delay}", attempts, delay);
    }
}
