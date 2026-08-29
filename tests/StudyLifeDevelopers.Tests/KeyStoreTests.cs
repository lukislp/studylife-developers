using Microsoft.Extensions.Configuration;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests;

public class KeyStoreTests : IDisposable
{
    private readonly string _dataDir;
    private readonly KeyStore _store;

    public KeyStoreTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "sld-tests-" + Guid.NewGuid());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataDir"] = _dataDir })
            .Build();
        _store = new KeyStore(config);
    }

    [Fact]
    public void GetKey_BeforeAnythingStored_ReturnsNull()
    {
        Assert.Null(_store.GetKey());
    }

    [Fact]
    public void SetKeyThenGetKey_RoundTrips()
    {
        _store.SetKey("plaintext-key-123");

        Assert.Equal("plaintext-key-123", _store.GetKey());
    }

    [Fact]
    public void SetKeyTwice_OverwritesTheFirst()
    {
        _store.SetKey("first");
        _store.SetKey("second");

        Assert.Equal("second", _store.GetKey());
    }

    [Fact]
    public void Clear_RemovesTheStoredKey()
    {
        _store.SetKey("some-key");

        _store.Clear();

        Assert.Null(_store.GetKey());
    }

    [Fact]
    public void Clear_WithNothingStored_DoesNotThrow()
    {
        _store.Clear();
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, recursive: true);
    }
}
