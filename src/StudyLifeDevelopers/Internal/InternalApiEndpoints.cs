using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Internal;

/// <summary>
/// The two endpoints the paired StudyAtrium instance calls (DeveloperProxyClient.RegisterKeyAsync/
/// RevokeKeyAsync there), authenticated by <see cref="SharedSecretAuthenticationHandler"/>. They
/// are served on the internal listener only (see Hosting.ListenerSeparationMiddleware): on the
/// public port that the gateway route reaches, /internal/* does not exist. StudyAtrium never
/// impersonates a specific user here; this portal is single-tenant, so the received key is
/// simply THE key (see KeyStore).
/// </summary>
public static class InternalApiEndpoints
{
    public const string PathPrefix = "/internal";
    public const string PolicyName = "InternalApi";

    public static IServiceCollection AddInternalApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InternalApiOptions>(configuration.GetSection(InternalApiOptions.SectionName));
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, SharedSecretAuthenticationHandler>(SharedSecretAuthenticationHandler.SchemeName, null);
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyName, policy => policy
                .AddAuthenticationSchemes(SharedSecretAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser());
        return services;
    }

    public static IEndpointRouteBuilder MapInternalApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PathPrefix).RequireAuthorization(PolicyName);

        group.MapPost("/register-key", (RegisterKeyRequest body, KeyStore keyStore) =>
        {
            keyStore.SetKey(body.ApiKey);
            return Results.Ok();
        });

        group.MapPost("/revoke-key", (KeyStore keyStore) =>
        {
            keyStore.Clear();
            return Results.Ok();
        });

        return endpoints;
    }
}
