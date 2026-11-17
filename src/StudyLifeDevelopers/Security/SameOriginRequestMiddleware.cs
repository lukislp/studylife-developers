namespace StudyLifeDevelopers.Security;

/// <summary>
/// Refuses cross-origin state-changing requests: every non-safe method (POST, PUT, DELETE, ...)
/// and every WebSocket upgrade (the Blazor Server circuit at /_blazor) whose <c>Origin</c> header
/// names a different host than the request itself gets a 403. The session cookie is
/// SameSite=Lax, which already keeps it off cross-site requests, but "site" is the registrable
/// domain: a sibling app on another subdomain of the same domain counts as same-site. This check
/// closes that gap for the circuit, where the classic form antiforgery token does not apply.
/// Requests without an Origin header (non-browser clients, e.g. StudyAtrium's server calling
/// /internal/*) are not affected; they never carry the browser's cookie.
/// </summary>
public sealed class SameOriginRequestMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (NeedsCheck(context) && !IsSameOrigin(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        return next(context);
    }

    private static bool NeedsCheck(HttpContext context) =>
        context.WebSockets.IsWebSocketRequest
        || !(HttpMethods.IsGet(context.Request.Method)
             || HttpMethods.IsHead(context.Request.Method)
             || HttpMethods.IsOptions(context.Request.Method)
             || HttpMethods.IsTrace(context.Request.Method));

    /// <summary>Compares host names only: behind the gateway the request reaches this pod over
    /// plain HTTP on another port than the browser used, so scheme and port legitimately differ.
    /// The Host header itself is pinned by AllowedHosts.</summary>
    public static bool IsSameOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
            return true;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return false; // includes the opaque "null" origin
        return string.Equals(originUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase);
    }
}
