using PartnerCommission.Contracts;
using System.Net;

namespace PartnerCommission.Commissions.Api.Services;

internal sealed class PartnersClient(
    HttpClient httpClient,
    ILogger<PartnersClient> logger
    ) : IPartnersClient
{
    public async Task<AncestorsResponse?> GetAncestorsAsync(string userExternalId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userExternalId);

        using var response = await httpClient.GetAsync($"internal/users/{Uri.EscapeDataString(userExternalId)}/ancestors", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("User {UserExternalId} not found in Partners", userExternalId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AncestorsResponse>(ct)
            ?? throw new InvalidOperationException("Partners returned an empty response body");

        return body;
    }
}