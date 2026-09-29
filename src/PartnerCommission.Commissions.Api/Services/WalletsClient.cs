using PartnerCommission.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

internal sealed class WalletsClient(HttpClient httpClient) : IWalletsClient
{
    public async Task SendCommissionAccruedAsync(CommissionAccruedMessage message, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("internal/commissions", message, ct);

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<CommissionPaymentResponse>> GetCommissionPaymentsAsync(IReadOnlyCollection<Guid> commissionIds, CancellationToken ct)
    {
        if (commissionIds.Count == 0)
            return [];

        var query = new CommissionPaymentsQuery(commissionIds.ToList());

        using var response = await httpClient.PostAsJsonAsync("internal/commissions/payments:query", query, ct);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<CommissionPaymentResponse>>(ct)
            ?? throw new InvalidOperationException("Wallets returned an empty response body");

        return body;
    }
}
