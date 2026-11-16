using System.Net.Http.Json;
using System.Text.Json;

namespace StudyLifeDevelopers.Services;

/// <summary>
/// Redeems a sign-in assertion at the paired StudyAtrium instance's generic connect flow
/// (POST /api/auth/assertion-exchange, anonymous: the single-use assertion plus the PKCE
/// verifier are the credential). Server-to-server over the same cluster-internal StudyLife:BaseUrl
/// and CA trust as <see cref="StudyLifeApiClient"/>, never through the browser.
/// </summary>
public sealed class StudyLifeSignInClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The StudyAtrium user id the assertion was issued to, or null when StudyAtrium
    /// refuses it (unknown, expired, already used, wrong client or wrong verifier). The API key
    /// that comes back with it is deliberately dropped: the portal only needs the identity.</summary>
    public async Task<int?> RedeemAssertionAsync(string clientId, string assertion, string codeVerifier, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/assertion-exchange",
            new AssertionExchangeRequestDto { ClientId = clientId, Assertion = assertion, CodeVerifier = codeVerifier },
            JsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<AssertionExchangeResponseDto>(JsonOptions, ct);
        return result is { UserId: > 0 } ? result.UserId : null;
    }
}
