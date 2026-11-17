using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// Wires the portal's sign-in: cookie session, a deny-by-default authorization fallback (every
/// endpoint without its own rule needs a valid portal session), the sign-in rate limit, the
/// circuit revalidation and a Data Protection key ring that survives restarts.
/// </summary>
public static class PortalAuthServiceCollectionExtensions
{
    public static IServiceCollection AddPortalSignIn(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PortalAuthOptions>()
            .Bind(configuration.GetSection(PortalAuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PortalAuthOptions>, PortalAuthOptionsValidator>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PortalSession>();
        services.AddSingleton<SignInCorrelation>();
        services.AddScoped<PortalClientRegistrar>();
        services.AddScoped<PortalSignInService>();

        AddSessionCookie(services);
        AddKeyRing(services);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(PortalSession.Scheme)
                .RequireAuthenticatedUser()
                .RequireClaim(PortalSession.PairingClaim)
                .Build());

        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, PortalAuthenticationStateProvider>();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(PortalSignInEndpoints.RateLimitPolicy, context =>
            {
                var permits = context.RequestServices.GetRequiredService<IOptions<PortalAuthOptions>>().Value.SignInPermitsPerMinute;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });
        return services;
    }

    private static void AddSessionCookie(IServiceCollection services)
    {
        services.AddAuthentication(PortalSession.Scheme).AddCookie(PortalSession.Scheme);
        services.AddOptions<CookieAuthenticationOptions>(PortalSession.Scheme)
            .Configure<IOptions<PortalAuthOptions>>((cookie, portal) =>
            {
                // __Host- prefix: the browser only accepts it Secure, on Path=/ and without a
                // Domain, so no sibling subdomain can set or read it.
                cookie.Cookie.Name = PortalSession.CookieName;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // Lax, not Strict: the sign-in finishes with a top-level redirect from the
                // StudyAtrium origin. Cross-site sub-requests and cross-site POSTs never carry
                // it; SameOriginRequestMiddleware covers same-site siblings.
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.Path = "/";
                cookie.Cookie.IsEssential = true;
                cookie.ExpireTimeSpan = TimeSpan.FromHours(portal.Value.SessionHours);
                cookie.SlidingExpiration = true;
                cookie.LoginPath = PortalSignInEndpoints.LoginPath;
                cookie.LogoutPath = PortalSignInEndpoints.LogoutPath;
                cookie.AccessDeniedPath = PortalSignInEndpoints.DeniedPath;
                cookie.ReturnUrlParameter = "returnUrl";
                cookie.Events.OnValidatePrincipal = async context =>
                {
                    var session = context.HttpContext.RequestServices.GetRequiredService<PortalSession>();
                    if (session.IsValid(context.Principal))
                        return;
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(PortalSession.Scheme);
                };
            });
    }

    /// <summary>The cookie is encrypted with the Data Protection key ring. Kept next to the key
    /// store in DataDir (the persistent volume) so a restart or rollout does not sign the operator
    /// out; same at-rest trust level as the developer key stored there.</summary>
    private static void AddKeyRing(IServiceCollection services)
    {
        services.AddDataProtection().SetApplicationName("studylife-developers");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IConfiguration, ILoggerFactory>((keys, configuration, loggers) =>
            {
                var directory = new DirectoryInfo(Path.Combine(configuration["DataDir"] ?? "data", "dataprotection-keys"));
                keys.XmlRepository = new FileSystemXmlRepository(directory, loggers);
            });
    }
}
