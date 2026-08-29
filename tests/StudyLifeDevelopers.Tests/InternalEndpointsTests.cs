using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// /internal/register-key and /internal/revoke-key - the only endpoints studylife's own
/// DeveloperProxyClient ever calls (X-StudyLife-Shared-Secret authenticated, see
/// StudyLifeDevelopers:SharedSecret). Own factory per test class (IClassFixture would share one
/// KeyStore file across tests) - each test gets a fresh temp DataDir.
/// </summary>
public class InternalEndpointsTests : IDisposable
{
    private const string SharedSecret = "test-shared-secret";
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sld-tests-" + Guid.NewGuid());
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public InternalEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StudyLifeDevelopers:SharedSecret", SharedSecret);
            builder.UseSetting("DataDir", _dataDir);
        });
        _client = _factory.CreateClient();
    }

    private KeyStore KeyStore => _factory.Services.GetRequiredService<KeyStore>();

    [Fact]
    public async Task RegisterKey_WithCorrectSharedSecret_StoresTheKey()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/register-key")
        {
            Content = JsonContent.Create(new { user_id = 1, api_key = "real-key" }),
        };
        request.Headers.Add("X-StudyLife-Shared-Secret", SharedSecret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("real-key", KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_WithWrongSharedSecret_ReturnsUnauthorizedAndDoesNotStore()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/register-key")
        {
            Content = JsonContent.Create(new { user_id = 1, api_key = "should-not-be-stored" }),
        };
        request.Headers.Add("X-StudyLife-Shared-Secret", "wrong-secret");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RegisterKey_WithoutSharedSecretHeader_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/internal/register-key", new { user_id = 1, api_key = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeKey_WithCorrectSharedSecret_ClearsAnAlreadyStoredKey()
    {
        KeyStore.SetKey("existing-key");
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/revoke-key");
        request.Headers.Add("X-StudyLife-Shared-Secret", SharedSecret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(KeyStore.GetKey());
    }

    [Fact]
    public async Task RevokeKey_WithWrongSharedSecret_ReturnsUnauthorizedAndLeavesTheKeyIntact()
    {
        KeyStore.SetKey("existing-key");
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/revoke-key");
        request.Headers.Add("X-StudyLife-Shared-Secret", "wrong");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("existing-key", KeyStore.GetKey());
    }

    [Fact]
    public async Task Health_ReturnsOkWithoutAnyAuthentication()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, recursive: true);
    }
}
