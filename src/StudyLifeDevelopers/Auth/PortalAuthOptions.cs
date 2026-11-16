using Microsoft.Extensions.Options;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// Sign-in settings (section "PortalAuth"). The portal signs its operator in through the paired
/// StudyAtrium instance's own consent flow and only admits the account the portal is paired
/// with, see <see cref="PortalSignInService"/>.
/// </summary>
public sealed class PortalAuthOptions
{
    public const string SectionName = "PortalAuth";
    public const string CallbackPath = "/auth/callback";

    /// <summary>Canonical external URL of this portal as the browser sees it, for example
    /// https://studylife-developers.example.com. The sign-in callback is
    /// {PublicUrl}/auth/callback; a sign-in started on any other allowed host name is first
    /// redirected here, since the session cookie is host-only.</summary>
    public string PublicUrl { get; set; } = "";

    /// <summary>Browser-facing URL of the paired StudyAtrium instance (its web UI, not the
    /// cluster-internal StudyLife:BaseUrl). The browser is sent to its /connect/client page.</summary>
    public string StudyLifePublicUrl { get; set; } = "";

    /// <summary>OAuth client id the portal registers for its own sign-in on the paired instance
    /// (through the developer key it already holds). Client ids are unique per instance; if
    /// another account already owns this one, sign-in refuses to use it and this setting has to
    /// be changed.</summary>
    public string ClientId { get; set; } = "studylife-developers-portal";

    /// <summary>Lifetime of a session, sliding: renewed while the portal is in use.</summary>
    public double SessionHours { get; set; } = 8;

    /// <summary>Sign-in requests (/auth/login and /auth/callback) allowed per client address per
    /// minute.</summary>
    public int SignInPermitsPerMinute { get; set; } = 20;

    public Uri PublicUri => new(PublicUrl, UriKind.Absolute);

    public Uri CallbackUri => new(PublicUri, CallbackPath);

    public Uri StudyLifePublicUri => new(StudyLifePublicUrl, UriKind.Absolute);
}

/// <summary>Fails the start on a missing or malformed sign-in configuration: without it the
/// portal could not authenticate anyone, and it must never fall back to serving the UI
/// unauthenticated.</summary>
public sealed class PortalAuthOptionsValidator : IValidateOptions<PortalAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, PortalAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var errors = new List<string>();

        if (!Uri.TryCreate(options.PublicUrl, UriKind.Absolute, out var publicUri) || !IsAllowedCallbackOrigin(publicUri))
            errors.Add($"{PortalAuthOptions.SectionName}:PublicUrl must be the portal's absolute https URL " +
                       "(http is accepted for localhost/127.0.0.1 only, like StudyAtrium's redirect URI rule).");
        else if (publicUri.AbsolutePath != "/" || !string.IsNullOrEmpty(publicUri.Query))
            errors.Add($"{PortalAuthOptions.SectionName}:PublicUrl must be an origin without a path or query.");

        if (!Uri.TryCreate(options.StudyLifePublicUrl, UriKind.Absolute, out var studyLifeUri)
            || (studyLifeUri.Scheme != Uri.UriSchemeHttps && studyLifeUri.Scheme != Uri.UriSchemeHttp))
            errors.Add($"{PortalAuthOptions.SectionName}:StudyLifePublicUrl must be the absolute URL of the paired StudyAtrium web UI.");

        if (string.IsNullOrEmpty(options.ClientId) || options.ClientId.Length > 100
            || !options.ClientId.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-'))
            errors.Add($"{PortalAuthOptions.SectionName}:ClientId must be a lowercase alphanumeric-and-hyphen slug, at most 100 characters.");

        if (options.SessionHours <= 0)
            errors.Add($"{PortalAuthOptions.SectionName}:SessionHours must be positive.");
        if (options.SignInPermitsPerMinute <= 0)
            errors.Add($"{PortalAuthOptions.SectionName}:SignInPermitsPerMinute must be positive.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    /// <summary>Mirrors StudyAtrium's ConsentRedirectPolicy.IsAllowedRedirectUri: https, or the
    /// RFC 8252 loopback exception.</summary>
    private static bool IsAllowedCallbackOrigin(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps
        || (uri.Scheme == Uri.UriSchemeHttp
            && (string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal)
                || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)));
}
