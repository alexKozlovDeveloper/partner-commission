using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;
using PartnerCommission.Commissions.Api.Services;
using PartnerCommission.Commissions.Domain;
using System.Text.Json;

namespace PartnerCommission.Commissions.Api.Background;

internal sealed class ProfitEventHandler(
    CommissionsDbContext db,
    IPartnersClient partnersClient,
    ICommissionCalculator calculator,
    ILogger<ProfitEventHandler> logger)
{
    private const int MaxAttempts = 10;

    public async Task HandleAsync(Guid profitEventId, CancellationToken ct)
    {
        //using var _ = logger.BeginScope(new Dictionary<string, object> { ["EventExternalId"] = evt.EventExternalId });

        var profitEvent = await db.ProfitEvents
            .Where(x => x.Id == profitEventId)
            .SingleAsync(ct);

        try
        {
            await ProcessAsync(profitEvent, ct);            

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
        }
    }

    private async Task ProcessAsync(ProfitEvent profitEvent, CancellationToken ct)
    {
        if (profitEvent.Profit > 0)
        {
            var ancestors = await partnersClient.GetAncestorsAsync(profitEvent.UserExternalId, ct);

            if (ancestors is not null)
            {
                var now = DateTime.UtcNow;

                var beneficiary = ancestors.Ancestors
                    .Select(x => new BeneficiaryLine(x.ExternalId, x.Level))
                    .ToList();

                var lines = calculator.Calculate(profitEvent.Profit, profitEvent.SchemaType, beneficiary);

                foreach (var line in lines)
                {
                    var commission = new Commission
                    {
                        Id = Guid.NewGuid(),
                        ProfitEventId = profitEvent.EventExternalId,
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
                        Type = nameof(CommissionAccruedMessage),
                        Payload = JsonSerializer.Serialize(payload),
                        CreatedAtUtc = now
                    };

                    db.OutboxMessages.Add(message);
                }

                profitEvent.Status = ProfitEventStatus.Processed;
            }
            else
            {
                profitEvent.Status = ProfitEventStatus.Unresolved;                
            }
        }
        else 
        {
            profitEvent.Status = ProfitEventStatus.Processed;
        }
        
        profitEvent.ProcessedAtUtc = DateTime.UtcNow;
        profitEvent.LastError = null;

        await db.SaveChangesAsync(ct);
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