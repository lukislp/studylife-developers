using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using StudyLifeDevelopers.Security;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// The portal session: who signed in, and which pairing they signed in under. The pairing claim
/// is a fingerprint of the developer key StudyAtrium registered; revoking the connection or
/// pairing again (a new key) changes it, which ends every existing session on the next request
/// or circuit revalidation without any server-side session store.
/// </summary>
public sealed class PortalSession(KeyStore keyStore)
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string CookieName = "__Host-studylife-developers";
    public const string PairingClaim = "studylife-developers:pairing";

    public ClaimsPrincipal CreatePrincipal(int userId)
    {
        var key = keyStore.GetKey() ?? throw new InvalidOperationException("No pairing to sign in under.");
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(PairingClaim, Fingerprint(key)),
        ], Scheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>True while the principal's pairing is still the current one.</summary>
    public bool IsValid(ClaimsPrincipal? principal)
    {
        var claimed = principal?.FindFirst(PairingClaim)?.Value;
        var key = keyStore.GetKey();
        return claimed is not null && key is not null && SecretComparer.FixedTimeEquals(claimed, Fingerprint(key));
    }

    /// <summary>Domain-separated SHA-256 of the key; the key itself never goes into the cookie
    /// (which is encrypted anyway).</summary>
    internal static string Fingerprint(string key) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes("studylife-developers/pairing/v1\n" + key)));
}
