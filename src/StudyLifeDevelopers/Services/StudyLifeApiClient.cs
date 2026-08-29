using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudyLifeDevelopers.Services;

/// <summary>
/// Calls the paired studylife instance's /api/developer/clients endpoints, authenticated with
/// the DeveloperApiKeyHash-slot key this portal received from it (see KeyStore, /internal/
/// register-key). BaseAddress comes from config (StudyLife:BaseUrl) - exactly the same
/// "paired to your own instance via config" pattern every other satellite in this ecosystem
/// uses (STUDYLIFE_CONNECT_URL for mcp/hacs, StudyLifeWebhooks:BaseUrl on the studylife side
/// itself, etc.).
/// </summary>
public sealed class StudyLifeApiClient(HttpClient http, KeyStore keyStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>False when no key has been registered yet (the operator hasn't enabled the
    /// "studylife-developers" connection on their StudyLife Setup page) - callers show a
    /// "not connected" state instead of attempting any request.</summary>
    public bool Connected => keyStore.GetKey() is not null;

    private HttpRequestMessage NewRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var key = keyStore.GetKey();
        if (key is not null) request.Headers.Add("X-Api-Key", key);
        return request;
    }

    public async Task<List<DeveloperClientDto>> GetClientsAsync(CancellationToken ct = default)
    {
        using var request = NewRequest(HttpMethod.Get, "api/developer/clients");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DeveloperClientDto>>(JsonOptions, ct) ?? [];
    }

    public async Task<(bool Success, string? Error, DeveloperClientDto? Client)> CreateClientAsync(
        CreateDeveloperClientRequestDto body, CancellationToken ct = default)
    {
        using var request = NewRequest(HttpMethod.Post, "api/developer/clients");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync(ct), null);
        return (true, null, await response.Content.ReadFromJsonAsync<DeveloperClientDto>(JsonOptions, ct));
    }

    public async Task<(bool Success, string? Error, DeveloperClientDto? Client)> UpdateClientAsync(
        string clientId, UpdateDeveloperClientRequestDto body, CancellationToken ct = default)
    {
        using var request = NewRequest(HttpMethod.Put, $"api/developer/clients/{Uri.EscapeDataString(clientId)}");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return (false, await response.Content.ReadAsStringAsync(ct), null);
        return (true, null, await response.Content.ReadFromJsonAsync<DeveloperClientDto>(JsonOptions, ct));
    }

    public async Task<bool> DeleteClientAsync(string clientId, CancellationToken ct = default)
    {
        using var request = NewRequest(HttpMethod.Delete, $"api/developer/clients/{Uri.EscapeDataString(clientId)}");
        using var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}
