using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// HTTP surface of the sign-in: thin adapters around <see cref="PortalSignInService"/>. The two
/// round-trip endpoints are anonymous by nature and rate limited; sign-out is a POST that
/// requires the antiforgery token.
/// </summary>
public static class PortalSignInEndpoints
{
    public const string LoginPath = "/auth/login";
    public const string LogoutPath = "/auth/logout";
    public const string DeniedPath = "/auth/denied";
    public const string SignedOutPath = "/auth/signed-out";
    public const string RateLimitPolicy = "sign-in";

    public static IEndpointRouteBuilder MapPortalSignIn(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(LoginPath, LoginAsync).AllowAnonymous().RequireRateLimiting(RateLimitPolicy);
        endpoints.MapGet(PortalAuthOptions.CallbackPath, CallbackAsync).AllowAnonymous().RequireRateLimiting(RateLimitPolicy);
        endpoints.MapPost(LogoutPath, LogoutAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context, string? returnUrl, PortalSignInService signIn, SignInCorrelation correlation,
        IOptions<PortalAuthOptions> options, CancellationToken ct)
    {
        // The session cookie is host-only: a sign-in started on another allowed host name would
        // come back to the canonical callback without its correlation cookie. Move it first.
        var publicUri = options.Value.PublicUri;
        if (!string.Equals(context.Request.Host.Host, publicUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            var local = PortalSignInService.LocalReturnUrl(returnUrl);
            return Results.Redirect(new Uri(publicUri, LoginPath + QueryString.Create("returnUrl", local)).AbsoluteUri);
        }

        var begin = await signIn.BeginAsync(returnUrl, ct);
        if (begin.Failure is { } failure)
            return Denied(failure);
        correlation.Write(context.Response, begin.Pending!);
        return Results.Redirect(begin.RedirectTo!.AbsoluteUri);
    }

    private static async Task<IResult> CallbackAsync(
        HttpContext context, string? state, string? assertion, PortalSignInService signIn,
        SignInCorrelation correlation, CancellationToken ct)
    {
        var pending = correlation.Take(context.Request, context.Response);
        var completion = await signIn.CompleteAsync(pending, state, assertion, ct);
        if (completion.Failure is { } failure)
            return Denied(failure);
        await context.SignInAsync(PortalSession.Scheme, completion.Principal!);
        return Results.LocalRedirect(completion.ReturnUrl);
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();
        await context.SignOutAsync(PortalSession.Scheme);
        return Results.LocalRedirect(SignedOutPath);
    }

    private static IResult Denied(SignInFailure failure) =>
        Results.LocalRedirect(DeniedPath + QueryString.Create("reason", failure.ToSlug()));
}
