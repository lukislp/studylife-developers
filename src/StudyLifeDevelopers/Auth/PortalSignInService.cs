using System.Security.Claims;
using Microsoft.Extensions.Options;
using StudyLifeDevelopers.Security;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Auth;

/// <summary>Why a sign-in was refused. The slug is what the /auth/denied page receives.</summary>
public enum SignInFailure
{
    NotPaired,
    ClientIdTaken,
    StudyLifeUnavailable,
    InvalidRequest,
    AssertionRejected,
    WrongAccount,
}

public static class SignInFailureSlugs
{
    public static string ToSlug(this SignInFailure failure) => failure switch
    {
        SignInFailure.NotPaired => "not-paired",
        SignInFailure.ClientIdTaken => "client-id-taken",
        SignInFailure.StudyLifeUnavailable => "unavailable",
        SignInFailure.InvalidRequest => "invalid-request",
        SignInFailure.AssertionRejected => "rejected",
        SignInFailure.WrongAccount => "wrong-account",
        _ => "unknown",
    };
}

/// <summary>
/// Signs the operator in through the paired StudyAtrium instance, reusing its generic add-on
/// connect flow unchanged (consent page /connect/client/{clientId}, single-use assertion,
/// PKCE S256, server-to-server assertion-exchange):
///
/// 1. <see cref="BeginAsync"/>: the portal makes sure its own sign-in client is registered
///    (<see cref="PortalClientRegistrar"/>) and sends the browser to StudyAtrium's consent page
///    with a fresh state and PKCE challenge.
/// 2. StudyAtrium signs the user in there if needed, shows the consent and redirects back to
///    /auth/callback with the assertion.
/// 3. <see cref="CompleteAsync"/>: state check (constant time), assertion redeemed with the PKCE
///    verifier, and the resulting StudyAtrium user id must be exactly the account the developer
///    key belongs to (GET /api/auth/whoami). Any other StudyAtrium account is refused.
///
/// No StudyAtrium change is needed for any of this.
/// </summary>
public sealed class PortalSignInService(
    PortalClientRegistrar registrar,
    StudyLifeApiClient api,
    StudyLifeSignInClient signIn,
    PortalSession session,
    IOptions<PortalAuthOptions> options,
    ILogger<PortalSignInService> logger)
{
    public sealed record Begin(PendingSignIn? Pending, Uri? RedirectTo, SignInFailure? Failure);

    public sealed record Completion(ClaimsPrincipal? Principal, string ReturnUrl, SignInFailure? Failure);

    public async Task<Begin> BeginAsync(string? returnUrl, CancellationToken ct = default)
    {
        if (!api.Connected)
            return new Begin(null, null, SignInFailure.NotPaired);

        switch (await registrar.EnsureRegisteredAsync(ct))
        {
            case PortalClientRegistrar.Outcome.ClientIdTaken:
                logger.LogWarning("Sign-in client id {ClientId} is taken by another StudyAtrium account; set PortalAuth:ClientId to another value", options.Value.ClientId);
                return new Begin(null, null, SignInFailure.ClientIdTaken);
            case PortalClientRegistrar.Outcome.Failed:
                return new Begin(null, null, SignInFailure.StudyLifeUnavailable);
        }

        var settings = options.Value;
        var pending = new PendingSignIn(Pkce.NewRandomToken(), Pkce.NewVerifier(), LocalReturnUrl(returnUrl));
        var query = QueryString.Create(new KeyValuePair<string, string?>[]
        {
            new("redirect_uri", settings.CallbackUri.AbsoluteUri),
            new("state", pending.State),
            new("code_challenge", Pkce.ChallengeFor(pending.CodeVerifier)),
            new("code_challenge_method", "S256"),
        });
        var consent = new Uri(settings.StudyLifePublicUri, $"/connect/client/{Uri.EscapeDataString(settings.ClientId)}{query}");
        return new Begin(pending, consent, null);
    }

    public async Task<Completion> CompleteAsync(PendingSignIn? pending, string? state, string? assertion, CancellationToken ct = default)
    {
        if (pending is null || string.IsNullOrEmpty(assertion) || !SecretComparer.FixedTimeEquals(state, pending.State))
            return new Completion(null, "/", SignInFailure.InvalidRequest);

        int? userId;
        int? pairedUserId;
        try
        {
            userId = await signIn.RedeemAssertionAsync(options.Value.ClientId, assertion, pending.CodeVerifier, ct);
            if (userId is null)
                return new Completion(null, "/", SignInFailure.AssertionRejected);
            pairedUserId = await api.GetPairedUserIdAsync(ct);
        }
        catch (HttpRequestException)
        {
            return new Completion(null, "/", SignInFailure.StudyLifeUnavailable);
        }

        if (pairedUserId is null)
            return new Completion(null, "/", SignInFailure.NotPaired);
        if (userId != pairedUserId)
        {
            logger.LogWarning("Refused sign-in of StudyAtrium user {UserId}: this portal is paired with another account", userId);
            return new Completion(null, "/", SignInFailure.WrongAccount);
        }
        return new Completion(session.CreatePrincipal(userId.Value), pending.ReturnUrl, null);
    }

    /// <summary>Only same-site relative paths survive; anything else (absolute URLs,
    /// protocol-relative //host, backslash tricks) becomes "/", so the sign-in can never be
    /// turned into an open redirect.</summary>
    public static string LocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || returnUrl[0] != '/')
            return "/";
        if (returnUrl.Length > 1 && (returnUrl[1] == '/' || returnUrl[1] == '\\'))
            return "/";
        return returnUrl.Any(char.IsControl) ? "/" : returnUrl;
    }
}
