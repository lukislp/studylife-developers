using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace StudyLifeDevelopers.Auth;

/// <summary>What the portal has to remember between sending the browser to StudyAtrium and the
/// browser coming back: the state it expects, the PKCE verifier and where to go afterwards.</summary>
public sealed record PendingSignIn(string State, string CodeVerifier, string ReturnUrl);

/// <summary>
/// Carries <see cref="PendingSignIn"/> across the round trip in a short-lived cookie, encrypted
/// and authenticated with ASP.NET Core Data Protection and time-limited, so nothing about the
/// attempt is kept server-side and the PKCE verifier never reaches the browser in readable form.
/// SameSite=Lax rather than Strict on purpose: the callback is a top-level navigation coming from
/// the StudyAtrium origin, and a Strict cookie would not be sent with it. The cookie is consumed
/// (deleted) on the first callback.
/// </summary>
public sealed class SignInCorrelation(IDataProtectionProvider dataProtection, TimeProvider time)
{
    public const string CookieName = "__Host-studylife-developers-signin";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ITimeLimitedDataProtector _protector =
        dataProtection.CreateProtector("StudyLifeDevelopers.Auth.SignInCorrelation.v1").ToTimeLimitedDataProtector();

    public void Write(HttpResponse response, PendingSignIn pending)
    {
        ArgumentNullException.ThrowIfNull(response);
        var payload = _protector.Protect(JsonSerializer.Serialize(pending), time.GetUtcNow() + Lifetime);
        response.Cookies.Append(CookieName, payload, CookieOptions(time.GetUtcNow() + Lifetime));
    }

    /// <summary>Reads and deletes the cookie. Null when it is missing, expired or tampered with.</summary>
    public PendingSignIn? Take(HttpRequest request, HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        if (!request.Cookies.TryGetValue(CookieName, out var payload) || string.IsNullOrEmpty(payload))
            return null;
        response.Cookies.Delete(CookieName, CookieOptions(expires: null));
        try
        {
            return JsonSerializer.Deserialize<PendingSignIn>(_protector.Unprotect(payload));
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or JsonException)
        {
            return null;
        }
    }

    // __Host- prefix: Secure, Path=/ and no Domain are required by the browser for the cookie to
    // be accepted at all, so it can neither be set nor read by a sibling subdomain.
    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expires,
        IsEssential = true,
    };
}
