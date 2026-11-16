using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// A Blazor Server circuit outlives the HTTP request that authenticated it, so the cookie check
/// alone would let an open tab keep working after the pairing was revoked or replaced. This
/// re-checks the session's pairing every minute; once it no longer holds, the router's
/// AuthorizeRouteView stops rendering the pages and sends the browser to sign in again.
/// </summary>
public sealed class PortalAuthenticationStateProvider(ILoggerFactory loggerFactory, PortalSession session)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);
        return Task.FromResult(session.IsValid(authenticationState.User));
    }
}
