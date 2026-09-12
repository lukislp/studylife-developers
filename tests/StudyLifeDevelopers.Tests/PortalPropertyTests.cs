using System.Net;
using System.Net.Http.Json;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// Property-based tests (FsCheck) for the two things this portal must get right for every
/// possible input, not just the examples in the other test classes: the key store hands back
/// exactly what studylife registered, and a wrong shared secret is refused whatever it looks like.
/// </summary>
public class PortalPropertyTests : IDisposable
{
    private const string SharedSecret = "test-shared-secret";
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sld-props-" + Guid.NewGuid());
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PortalPropertyTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StudyLifeDevelopers:SharedSecret", SharedSecret);
            builder.UseSetting("DataDir", _dataDir);
        });
        _client = _factory.CreateClient();
    }

    private KeyStore KeyStore => _factory.Services.GetRequiredService<KeyStore>();

    [Property(MaxTest = 200)]
    public bool The_key_store_hands_back_exactly_the_key_that_was_stored(NonNull<string> apiKey)
    {
        if (!IsWellFormed(apiKey.Get))
            return true;

        var store = KeyStore;
        store.SetKey(apiKey.Get);
        var roundTripped = store.GetKey();
        store.Clear();

        return roundTripped == apiKey.Get && store.GetKey() is null;
    }

    [Property(MaxTest = 50)]
    public bool A_wrong_shared_secret_is_always_refused_and_stores_nothing(NonNull<string> secret)
    {
        // Header values are ASCII and get trimmed on the wire, so only those variants are
        // meaningful - and a value that trims down to the real secret is not a wrong one.
        var provided = secret.Get;
        if (provided.Trim() == SharedSecret || !provided.All(c => c is >= ' ' and <= '~'))
            return true;

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/register-key")
        {
            Content = JsonContent.Create(new { user_id = 1, api_key = "never-stored" }),
        };
        request.Headers.TryAddWithoutValidation("X-StudyLife-Shared-Secret", provided);

        var response = _client.SendAsync(request).GetAwaiter().GetResult();

        return response.StatusCode == HttpStatusCode.Unauthorized && KeyStore.GetKey() is null;
    }

    private static bool IsWellFormed(string value) =>
        Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value)) == value;

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, recursive: true);
    }
}
