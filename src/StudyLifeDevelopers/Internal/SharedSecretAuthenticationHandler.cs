using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using StudyLifeDevelopers.Security;

namespace StudyLifeDevelopers.Internal;

/// <summary>
/// Authenticates the paired StudyAtrium server on /internal/* by the X-StudyLife-Shared-Secret
/// header, compared in constant time against StudyLifeDevelopers:SharedSecret. Runs as an
/// authentication scheme rather than inside the endpoint so a wrong or missing secret is refused
/// (401) before the request body is even read. An empty configured secret refuses everything.
/// </summary>
public sealed class SharedSecretAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<InternalApiOptions> internalApi)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "InternalSharedSecret";
    public const string HeaderName = "X-StudyLife-Shared-Secret";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var provided))
            return Task.FromResult(AuthenticateResult.NoResult());
        if (!SecretComparer.FixedTimeEquals(provided.ToString(), internalApi.CurrentValue.SharedSecret))
            return Task.FromResult(AuthenticateResult.Fail("Shared secret mismatch."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "studylife-server")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
