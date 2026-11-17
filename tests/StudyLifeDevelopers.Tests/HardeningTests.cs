using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StudyLifeDevelopers.Auth;
using StudyLifeDevelopers.Hosting;
using StudyLifeDevelopers.Security;
using StudyLifeDevelopers.Tests.Infrastructure;

namespace StudyLifeDevelopers.Tests;

/// <summary>Host pinning, the constant-time secret comparison, the origin check, the listener
/// layout and the fail-closed start (audit 2026-11-16, finding M6).</summary>
public sealed class HardeningTests : IDisposable
{
    private readonly PortalFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ── AllowedHosts ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/")]
    [InlineData("/auth/login")]
    [InlineData("/health")]
    public async Task An_unknown_host_header_is_refused(string path)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        // What a DNS-rebinding page sends: the attacker's own host name.
        request.Headers.Host = "rebind.attacker.example";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_allowed_host_header_is_served()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "alt.localhost";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void The_shipped_default_is_not_a_wildcard()
    {
        var settings = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .Build();

        Assert.NotEqual("*", settings["AllowedHosts"]);
        Assert.False(string.IsNullOrWhiteSpace(settings["AllowedHosts"]));
    }

    [Fact]
    public async Task Without_an_AllowedHosts_setting_only_localhost_is_served()
    {
        using var factory = new PortalFactory(new() { ["AllowedHosts"] = "" });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "rebind.attacker.example";

        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/health", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public void A_wildcard_AllowedHosts_refuses_to_start_outside_development()
    {
        using var factory = new PortalFactory(new() { ["AllowedHosts"] = "*", ["environment"] = "Production" });

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("AllowedHosts", ex.Message);
    }

    // ── Constant-time secret comparison ───────────────────────────────────────────────────

    [Theory]
    [InlineData("secret", "secret", true)]
    [InlineData("secreT", "secret", false)]
    [InlineData("secret-and-more", "secret", false)]
    [InlineData("sec", "secret", false)]
    [InlineData("", "secret", false)]
    [InlineData(null, "secret", false)]
    [InlineData("", "", false)]
    [InlineData("anything", null, false)]
    public void SecretComparer_matches_exactly_and_never_on_an_empty_expected_value(string? provided, string? expected, bool match) =>
        Assert.Equal(match, SecretComparer.FixedTimeEquals(provided, expected));

    [Fact]
    public void No_shared_secret_is_compared_with_plain_string_equality()
    {
        // Guards against a regression to "provided == secret": every shared-secret or state
        // comparison in the portal goes through SecretComparer.
        var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "StudyLifeDevelopers"));
        var offenders = Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select(line => (File: f, Line: line)))
            .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.Line, @"(?i)((secret|state)\w*\s*[!=]=\s*\w|[!=]=\s*\w*(secret|state))"))
            .ToList();

        Assert.Empty(offenders);
    }

    // ── Origin check ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, true)]
    [InlineData("https://portal.example", true)]
    [InlineData("http://portal.example:8080", true)]
    [InlineData("https://other.example", false)]
    [InlineData("https://sub.portal.example", false)]
    [InlineData("null", false)]
    public void Origin_must_name_the_requested_host(string? origin, bool allowed)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("portal.example");
        if (origin is not null) context.Request.Headers.Origin = origin;

        Assert.Equal(allowed, SameOriginRequestMiddleware.IsSameOrigin(context.Request));
    }

    // ── Return URL ────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/addons/new", "/addons/new")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("https://evil.example", "/")]
    [InlineData("addons", "/")]
    public void Only_local_return_urls_survive(string? input, string expected) =>
        Assert.Equal(expected, PortalSignInService.LocalReturnUrl(input));

    // ── Listener layout ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Without_tls_the_internal_port_defaults_to_8081()
    {
        var layout = ListenerLayout.From(Config([]));

        Assert.Equal([8080], layout.PublicPorts);
        Assert.Equal(8081, layout.InternalPort);
        Assert.False(layout.InternalUsesTls);
    }

    [Fact]
    public void With_tls_the_internal_port_is_the_https_port_StudyAtrium_already_calls()
    {
        var layout = ListenerLayout.From(Config(new() { ["ServerTls:CertPath"] = "a", ["ServerTls:KeyPath"] = "b" }));

        Assert.Equal(8443, layout.InternalPort);
        Assert.True(layout.InternalUsesTls);
    }

    [Fact]
    public void InternalApi_Port_overrides_the_default()
    {
        var layout = ListenerLayout.From(Config(new() { ["InternalApi:Port"] = "9000" }));

        Assert.Equal(9000, layout.InternalPort);
    }

    [Fact]
    public void The_internal_port_can_never_be_a_public_port()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ListenerLayout.From(Config(new() { ["HTTP_PORTS"] = "8080;8081", ["InternalApi:Port"] = "8081" })));

        Assert.Contains("must not be one of the public ports", ex.Message);
    }

    // ── Fail-closed configuration ─────────────────────────────────────────────────────────

    [Fact]
    public void The_portal_does_not_start_without_sign_in_configuration()
    {
        using var factory = new PortalFactory(new() { ["PortalAuth:PublicUrl"] = "", ["PortalAuth:StudyLifePublicUrl"] = "" });

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("PortalAuth:PublicUrl", ex.Message);
    }

    [Theory]
    [InlineData("http://portal.example")]
    [InlineData("https://portal.example/sub")]
    [InlineData("not a url")]
    public void A_public_url_StudyAtrium_would_not_accept_as_redirect_is_rejected(string publicUrl)
    {
        var result = new PortalAuthOptionsValidator().Validate(null, new PortalAuthOptions
        {
            PublicUrl = publicUrl,
            StudyLifePublicUrl = "https://studylife.example",
        });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("https://portal.example")]
    [InlineData("http://localhost:5080")]
    public void A_valid_public_url_is_accepted(string publicUrl)
    {
        var result = new PortalAuthOptionsValidator().Validate(null, new PortalAuthOptions
        {
            PublicUrl = publicUrl,
            StudyLifePublicUrl = "https://studylife.example",
        });

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
