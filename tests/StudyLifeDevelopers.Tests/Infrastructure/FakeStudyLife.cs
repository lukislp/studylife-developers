using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudyLifeDevelopers.Auth;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests.Infrastructure;

/// <summary>
/// In-memory stand-in for the paired StudyAtrium instance's API, mirroring the real endpoints'
/// behaviour that the portal relies on: whoami for the developer key, owner-filtered
/// /api/developer/clients (with a globally unique client id), and the single-use, PKCE-checked
/// /api/auth/assertion-exchange.
/// </summary>
public sealed class FakeStudyLife : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string DeveloperKey { get; set; } = "dev-key";
    public int PairedUserId { get; set; } = 7;
    public bool Unreachable { get; set; }

    public ConcurrentDictionary<string, DeveloperClientDto> OwnClients { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, byte> ClientIdsOwnedByOthers { get; } = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (int UserId, string ClientId, string Challenge)> _assertions = new(StringComparer.Ordinal);
    public ConcurrentBag<AssertionExchangeRequestDto> Exchanges { get; } = [];

    /// <summary>What StudyAtrium's consent page does after the user clicks "Connect": issue a
    /// single-use assertion bound to the client and the PKCE challenge it was started with.</summary>
    public string IssueAssertion(int userId, string clientId, string codeChallenge)
    {
        var assertion = Pkce.NewRandomToken();
        _assertions[assertion] = (userId, clientId, codeChallenge);
        return assertion;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Unreachable)
            throw new HttpRequestException("connection refused");
        var path = request.RequestUri!.AbsolutePath;
        var method = request.Method;

        if (path == "/api/auth/assertion-exchange" && method == HttpMethod.Post)
            return await ExchangeAsync(request, cancellationToken);

        if (!request.Headers.TryGetValues("X-Api-Key", out var keys) || keys.Single() != DeveloperKey)
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        if (path == "/api/auth/whoami" && method == HttpMethod.Get)
            return Ok(new WhoamiResponseDto { UserId = PairedUserId, Credential = "developer" });

        if (path == "/api/developer/clients" && method == HttpMethod.Get)
            return Ok(OwnClients.Values.ToList());

        if (path == "/api/developer/clients" && method == HttpMethod.Post)
        {
            var body = (await request.Content!.ReadFromJsonAsync<CreateDeveloperClientRequestDto>(Json, cancellationToken))!;
            if (ClientIdsOwnedByOthers.ContainsKey(body.ClientId) || OwnClients.ContainsKey(body.ClientId))
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"ClientId '{body.ClientId}' is already taken.") };
            var created = new DeveloperClientDto
            {
                ClientId = body.ClientId,
                Name = body.Name,
                Description = body.Description,
                AllowedRedirectUris = body.AllowedRedirectUris,
                RequestedScopes = body.RequestedScopes,
            };
            OwnClients[body.ClientId] = created;
            return Ok(created);
        }

        if (path.StartsWith("/api/developer/clients/", StringComparison.Ordinal) && method == HttpMethod.Put)
        {
            var id = Uri.UnescapeDataString(path["/api/developer/clients/".Length..]);
            if (!OwnClients.TryGetValue(id, out var existing))
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body = (await request.Content!.ReadFromJsonAsync<UpdateDeveloperClientRequestDto>(Json, cancellationToken))!;
            existing.Name = body.Name;
            existing.Description = body.Description;
            existing.AllowedRedirectUris = body.AllowedRedirectUris;
            existing.RequestedScopes = body.RequestedScopes;
            return Ok(existing);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> ExchangeAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = (await request.Content!.ReadFromJsonAsync<AssertionExchangeRequestDto>(Json, ct))!;
        Exchanges.Add(body);
        if (!_assertions.TryGetValue(body.Assertion, out var pending)
            || pending.ClientId != body.ClientId
            || body.CodeVerifier is null
            || Pkce.ChallengeFor(body.CodeVerifier) != pending.Challenge)
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _assertions.TryRemove(body.Assertion, out _);
        return Ok(new AssertionExchangeResponseDto { UserId = pending.UserId, ApiKey = "client-key-that-the-portal-drops" });
    }

    private static HttpResponseMessage Ok<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: Json) };
}
