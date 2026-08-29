using System.Text.Json;

namespace StudyLifeDevelopers.Services;

/// <summary>
/// Holds the single DeveloperApiKeyHash-slot plaintext this portal received from its paired
/// studylife instance (see the /internal/register-key endpoint) - exactly one key at a time,
/// since this portal is single-tenant: one deployment paired to exactly one studylife instance,
/// exactly like every other satellite in this ecosystem. Deliberately a plain JSON file rather
/// than a database - there is only ever one row, a full SQLite/EF setup would be pure overhead
/// for it. Not encrypted at rest (unlike studylife-ai's registered_keys.db, which holds MANY
/// users' external-provider keys): this file holds one key, and it only ever grants access to
/// DeveloperController (managing one's own add-on registrations), never any study data - a
/// deliberately lower-stakes secret than what justified encryption there.
/// </summary>
public sealed class KeyStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public KeyStore(IConfiguration configuration)
    {
        var dataDir = configuration["DataDir"] ?? "data";
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "developer-key.json");
    }

    private sealed record StoredKey(string? ApiKey);

    public string? GetKey()
    {
        lock (_lock)
        {
            if (!File.Exists(_path)) return null;
            var stored = JsonSerializer.Deserialize<StoredKey>(File.ReadAllText(_path));
            return stored?.ApiKey;
        }
    }

    public void SetKey(string apiKey)
    {
        lock (_lock)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(new StoredKey(apiKey)));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
