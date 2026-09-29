using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Api.Observability;
using PartnerCommission.Commissions.Api.Services;
using PartnerCommission.Contracts;
using PartnerCommission.Shared.Diagnostics;
using PartnerCommission.Shared.Hosting;
using System.Text.Json;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class OutboxMessageHandler(
    CommissionsDbContext db,
    IWalletsClient walletsClient,
    ILogger<OutboxMessageHandler> logger)
{
    public async Task HandleAsync(Guid outboxMessageId, CancellationToken ct)
    {
        using var activity = Tracing.Source.StartActivity("DispatchOutboxMessage");

        var message = await db.OutboxMessages
            .Where(x => x.Id == outboxMessageId)
            .SingleAsync(ct);

        activity?.SetTag("outbox.message_id", message.Id);
        activity?.SetTag("outbox.message_type", message.Type);

        using var _ = logger.BeginScope("OutboxMessageId: {OutboxMessageId}, Type: {OutboxMessageType}", message.Id, message.Type);

        try
        {
            await ProcessAsync(message, ct);

            CommissionsMetrics.OutboxMessagesSent.WithLabels("sent").Inc();

            logger.LogInformation("Outbox message sent");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();

            await ScheduleRetryAsync(message.Id, message.Attempts + 1, ex, ct);

            CommissionsMetrics.OutboxMessagesSent.WithLabels("failed").Inc();
        }
    }

    private async Task ProcessAsync(OutboxMessage message, CancellationToken ct)
    {
        await SendAsync(message, ct);

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.LastError = null;

        await db.SaveChangesAsync(ct);
    }

    private async Task SendAsync(OutboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case CommissionAccruedMessage.MessageType:
                {
                    var payload = JsonSerializer.Deserialize<CommissionAccruedMessage>(message.Payload)
                        ?? throw new InvalidOperationException($"Outbox message {message.Id} of type '{message.Type}' has empty or null payload");

                    await walletsClient.SendCommissionAccruedAsync(payload, ct);

                    break;
                }

            default:
                throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'");
        }
    }

    private async Task ScheduleRetryAsync(Guid outboxMessageId, int attempts, Exception ex, CancellationToken ct)
    {
        var message = await db.OutboxMessages
            .Where(x => x.Id == outboxMessageId)
            .SingleAsync(ct);

        var delay = Retry.NextDelay(attempts);

        message.Attempts = attempts;
        message.LastError = Retry.ErrorText(ex);
        message.NextAttemptAtUtc = DateTime.UtcNow + delay;

        await db.SaveChangesAsync(ct);

        logger.LogWarning(ex, "Outbox message failed (attempt {Attempts}), retry in {Delay}", attempts, delay);
    }
}
