using PartnerCommission.Contracts;

namespace PartnerCommission.Commissions.Api.Services;

internal sealed class WalletsClient(HttpClient httpClient) : IWalletsClient
{
    public async Task SendCommissionAccruedAsync(CommissionAccruedMessage message, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync($"internal/commissions", message, ct);

        response.EnsureSuccessStatusCode();
    }
}