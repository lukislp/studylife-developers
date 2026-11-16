using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using StudyLifeDevelopers.Services;
using StudyLifeDevelopers.Tests.Infrastructure;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// /internal/register-key and /internal/revoke-key - the only endpoints studylife's own
/// DeveloperProxyClient ever calls (X-StudyLife-Shared-Secret authenticated, see
/// StudyLifeDevelopers:SharedSecret), served on the internal listener only. Own factory per test
/// class instance - each test gets a fresh temp DataDir.
/// </summary>
public sealed class InternalEndpointsTests : IDisposable
{
    private const string SharedSecret = PortalFactory.SharedSecret;
    private readonly PortalFactory _factory = new();
    private readonly HttpClient _client;

    public InternalEndpointsTests() => _client = _factory.CreateClient();

    private KeyStore KeyStore => _factory.KeyStore;

    private static HttpRequestMessage Internal(HttpMethod method, string path, string? secret, object? body = null, int port = PortalFactory.InternalPort)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (secret is not null) request.Headers.Add("X-StudyLife-Shared-Secret", secret);
        request.Headers.Add(PortalFactory.LocalPortHeader, port.ToString(CultureInfo.InvariantCulture));
        return request;
    }

    [Fact]
    public async Task RegisterKey_WithCorrectSharedSecret_StoresTheKey()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", SharedSecret, new { user_id = 1, api_key = "real-key" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("real-key", KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_WithWrongSharedSecret_ReturnsUnauthorizedAndDoesNotStore()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", "wrong-secret", new { user_id = 1, api_key = "should-not-be-stored" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_WithASecretThatOnlySharesAPrefix_IsRefused()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", SharedSecret + "x", new { user_id = 1, api_key = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_WithoutSharedSecretHeader_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", null, new { user_id = 1, api_key = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterKey_WithWrongSecretAndNoBody_IsRefusedBeforeTheBodyIsRead()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterKey_IsRefusedWhenNoSharedSecretIsConfigured()
    {
        using var factory = new PortalFactory(new() { ["StudyLifeDevelopers:SharedSecret"] = "" });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", "", new { user_id = 1, api_key = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_OnThePublicPort_DoesNotExistEvenWithTheRightSecret()
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/register-key", SharedSecret,
            new { user_id = 1, api_key = "via-the-gateway" }, PortalFactory.PublicPort));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RevokeKey_OnThePublicPort_DoesNotExistEvenWithTheRightSecret()
    {
        KeyStore.SetKey("existing-key");

        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/revoke-key", SharedSecret, port: PortalFactory.PublicPort));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("existing-key", KeyStore.GetKey());
    }

    [Fact]
    public async Task RevokeKey_WithCorrectSharedSecret_ClearsAnAlreadyStoredKey()
    {
        KeyStore.SetKey("existing-key");

        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/revoke-key", SharedSecret));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RevokeKey_WithWrongSharedSecret_ReturnsUnauthorizedAndLeavesTheKeyIntact()
    {
        KeyStore.SetKey("existing-key");

        var response = await _client.SendAsync(Internal(HttpMethod.Post, "/internal/revoke-key", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("existing-key", KeyStore.GetKey());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/addons/new")]
    [InlineData("/auth/login")]
    public async Task The_internal_port_serves_nothing_but_internal_and_health(string path)
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Get, path, SharedSecret));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PortalFactory.PublicPort)]
    [InlineData(PortalFactory.InternalPort)]
    public async Task Health_ReturnsOkWithoutAnyAuthenticationOnBothPorts(int port)
    {
        var response = await _client.SendAsync(Internal(HttpMethod.Get, "/health", null, port: port));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
