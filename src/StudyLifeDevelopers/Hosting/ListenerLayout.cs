using System.Globalization;

namespace StudyLifeDevelopers.Hosting;

/// <summary>
/// Which ports this process listens on and what each one is for:
///
/// - Public port(s), from ASPNETCORE_HTTP_PORTS (default 8080): the portal UI behind the
///   gateway route, plus /health for the kubelet probes. /internal/* does not exist here.
/// - Internal port, from InternalApi:Port: only /internal/* (StudyAtrium's server-to-server
///   calls) and /health. Never routed through the gateway. When ServerTls is configured this is
///   the HTTPS listener and defaults to ServerTls:Port (8443), the port StudyAtrium already
///   calls; without TLS it is plain HTTP and defaults to 8081.
///
/// Port 0 (an ephemeral port, tests only) is never treated as the internal port.
/// </summary>
public sealed record ListenerLayout(IReadOnlyList<int> PublicPorts, int InternalPort, bool InternalUsesTls)
{
    public const int DefaultPublicPort = 8080;
    public const int DefaultInternalPlainPort = 8081;
    public const string InternalPortKey = "InternalApi:Port";

    public static ListenerLayout From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var tls = ServerTls.IsConfigured(configuration);
        var publicPorts = ParseHttpPorts(configuration["HTTP_PORTS"]);
        var internalPort = configuration.GetValue<int?>(InternalPortKey)
            ?? (tls
                ? configuration.GetValue<int?>($"{ServerTls.SectionName}:Port") ?? ServerTls.DefaultPort
                : DefaultInternalPlainPort);
        if (internalPort != 0 && publicPorts.Contains(internalPort))
            throw new InvalidOperationException(
                $"{InternalPortKey} ({internalPort}) must not be one of the public ports (ASPNETCORE_HTTP_PORTS): " +
                "the internal API would become reachable through the gateway route.");
        return new ListenerLayout(publicPorts, internalPort, tls);
    }

    /// <summary>True when the connection arrived on the internal listener.</summary>
    public bool IsInternal(int localPort) => InternalPort != 0 && localPort == InternalPort;

    /// <summary>ASPNETCORE_HTTP_PORTS ("8080", "8080;8081"); 8080 when unset (the Dockerfile's
    /// value).</summary>
    public static IReadOnlyList<int> ParseHttpPorts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [DefaultPublicPort];
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.Parse(p, CultureInfo.InvariantCulture))
            .ToArray();
    }
}
