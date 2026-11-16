using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Options;

namespace StudyLifeDevelopers.Hosting;

/// <summary>
/// Pins the Host header (ASP.NET Core host filtering, setting "AllowedHosts", semicolon
/// separated). A request naming any other host gets a 400 before anything else runs, which is
/// what defeats DNS rebinding: a page on rebind.attacker.example that resolves to this portal
/// still sends its own host name.
///
/// The framework's own fallback for a missing setting is "*" (allow everything), so the default
/// lives here rather than only in appsettings.json: unset means "localhost", and "*" refuses to
/// start outside the Development environment.
/// </summary>
public static class HostPinning
{
    public const string ConfigKey = "AllowedHosts";
    public const string DefaultHost = "localhost";

    public static IServiceCollection AddHostPinning(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure (not PostConfigure): runs before the framework's own PostConfigure, which
        // only fills in "*" when the list is still empty.
        services.AddOptions<HostFilteringOptions>()
            .Configure(options => options.AllowedHosts = Parse(configuration[ConfigKey]))
            .Validate(options => environment.IsDevelopment() || !options.AllowedHosts.Contains("*"),
                $"{ConfigKey} must list the portal's host names; \"*\" is only accepted in Development.")
            .ValidateOnStart();
        return services;
    }

    public static IList<string> Parse(string? value)
    {
        var hosts = (value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return hosts.Length > 0 ? hosts : [DefaultHost];
    }
}
