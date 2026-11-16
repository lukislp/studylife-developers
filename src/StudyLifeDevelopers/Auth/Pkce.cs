using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace StudyLifeDevelopers.Auth;

/// <summary>RFC 7636 PKCE (S256) and the random state value of one sign-in attempt.</summary>
public static class Pkce
{
    /// <summary>32 random bytes, base64url: 43 characters, inside RFC 7636's 43-128 range.</summary>
    public static string NewVerifier() => NewRandomToken();

    public static string ChallengeFor(string verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        return WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    /// <summary>256-bit random value, base64url.</summary>
    public static string NewRandomToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
