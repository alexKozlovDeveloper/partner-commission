using Microsoft.EntityFrameworkCore;
using PartnerCommission.Commissions.Api.Contracts;
using PartnerCommission.Commissions.Api.Data;
using PartnerCommission.Commissions.Api.Entities;

namespace PartnerCommission.Commissions.Api.Services;

public class CommissionsService(
    CommissionsDbContext commissionsDbContext,
    ICommissionSchemaSettings commissionSchemaSettings,
    IWalletsClient walletsClient,
    ILogger<CommissionsService> logger
    ) : ICommissionsService
{
    public async Task ReciveProfitEventAsync(string externalId, CreateEventRequest request, CancellationToken ct)
    {
        var currentSchemaType = await commissionSchemaSettings.GetCurrentAsync(ct);

        var profitEvent = new ProfitEvent
        {
            Id = Guid.NewGuid(),
            Status = ProfitEventStatus.Received,
            UserExternalId = externalId,
            EventExternalId = request.EventExternalId,
            Profit = request.Profit,
            SchemaType = currentSchemaType,
            CreatedAtUtc = DateTime.UtcNow,
            Attempts = 0,
            NextAttemptAtUtc = DateTime.UtcNow            
        };

        commissionsDbContext.ProfitEvents.Add(profitEvent);

        await commissionsDbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProfitEventResponse>> GetProfitEventsAsync(string externalId, CancellationToken ct)
    {
        var result = await commissionsDbContext.ProfitEvents
            .Where(x => x.UserExternalId == externalId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ProfitEventResponse(
                x.EventExternalId,
                x.Profit,
                x.SchemaType,
                x.Status,
                x.CreatedAtUtc,
                x.ProcessedAtUtc
                ))
            .ToListAsync(ct);

        return result;
    }

    public async Task<ProfitEventDetailsResponse?> GetProfitEventAsync(string externalId, string eventExternalId, CancellationToken ct)
    {
        var profitEvent = await commissionsDbContext.ProfitEvents
            .Where(x => x.UserExternalId == externalId && x.EventExternalId == eventExternalId)
            .FirstOrDefaultAsync(ct);

        if (profitEvent is null)
            return null;

        var commissions = await commissionsDbContext.Commissions
            .Where(x => x.ProfitEventId == profitEvent.EventExternalId)
            .OrderBy(x => x.Level)
            .ToListAsync(ct);

        var commissionIds = commissions
            .Select(x => x.Id)
            .ToList();

        var payments = await GetPaymentsAsync(commissionIds, ct);

        var commissionDetails = commissions
            .Select(x =>
            {
                var paidAtUtc = payments?.GetValueOrDefault(x.Id);

                var paymentStatus = payments is null 
                    ? CommissionPaymentStatus.Unknown
                    : paidAtUtc is not null 
                        ? CommissionPaymentStatus.Paid
                        : CommissionPaymentStatus.Pending;

                return new CommissionDetailsResponse(
                    x.Id,
                    x.BeneficiaryExternalId,
                    x.Level,
                    x.Amount,
                    x.SchemaType,
                    paymentStatus,
                    paidAtUtc
                    );
            })
            .ToList();

        var result = new ProfitEventDetailsResponse(
            profitEvent.EventExternalId,
            profitEvent.UserExternalId,
            profitEvent.Profit,
            profitEvent.SchemaType,
            profitEvent.Status,
            profitEvent.CreatedAtUtc,
            profitEvent.ProcessedAtUtc,
            commissionDetails
            );

        return result;
    }

    private async Task<Dictionary<Guid, DateTime?>?> GetPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct)
    {
        if (commissionIds.Count == 0)
            return [];

        try
        {
            var payments = await walletsClient.GetCommissionPaymentsAsync(commissionIds, ct);

            var result = payments.ToDictionary(x => x.CommissionId, x => x.PaidAtUtc);

            return result;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to get commission payments from Wallets, payment status is unknown");

            return null;
        }
    }
}
