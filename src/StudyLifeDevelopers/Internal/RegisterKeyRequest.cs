using System.Text.Json.Serialization;

namespace StudyLifeDevelopers.Internal;

/// <summary>Body of POST /internal/register-key - matches DeveloperProxyClient.RegisterKeyAsync's
/// anonymous-object payload shape exactly (snake_case). UserId is accepted for
/// shape-compatibility but otherwise unused: the paired account is resolved authoritatively from
/// the key itself (GET /api/auth/whoami, see PairedAccount), and this portal is single-tenant,
/// there is only ever one key (see KeyStore).</summary>
internal sealed record RegisterKeyRequest(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("api_key")] string ApiKey);
