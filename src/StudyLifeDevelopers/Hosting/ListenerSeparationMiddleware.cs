using StudyLifeDevelopers.Internal;

namespace StudyLifeDevelopers.Hosting;

/// <summary>
/// Keeps the two listeners of <see cref="ListenerLayout"/> apart, by the local port the
/// connection actually arrived on (not the client-controlled Host header):
///
/// - on the internal port only /internal/* and /health exist, so the portal UI is never served
///   there;
/// - on every other port /internal/* does not exist, so the shared-secret endpoints cannot be
///   reached through the gateway route at all.
///
/// Anything else answers 404, indistinguishable from a path that was never mapped.
/// </summary>
public sealed class ListenerSeparationMiddleware(RequestDelegate next, ListenerLayout layout)
{
    public const string HealthPath = "/health";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var path = context.Request.Path;
        var isInternalPath = path.StartsWithSegments(InternalApiEndpoints.PathPrefix, StringComparison.OrdinalIgnoreCase);
        var allowed = layout.IsInternal(context.Connection.LocalPort)
            ? isInternalPath || path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase)
            : !isInternalPath;
        if (allowed)
            return next(context);
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}
