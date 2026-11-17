using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using StudyLifeDevelopers.Auth;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests.Infrastructure;

/// <summary>
/// The real portal (Program) on TestServer, with the paired StudyAtrium instance replaced by
/// <see cref="FakeStudyLife"/> and a fresh DataDir per factory. TestServer has no sockets, so the
/// local port a request "arrived on" is set from the X-Test-Local-Port header (default: the public
/// port) - that is the one input ListenerSeparationMiddleware decides on.
/// </summary>
public sealed class PortalFactory : WebApplicationFactory<Program>
{
    public const string SharedSecret = "test-shared-secret";
    public const int PublicPort = 8080;
    public const int InternalPort = 8081;
    public const string LocalPortHeader = "X-Test-Local-Port";
    public const string StudyLifePublicUrl = "https://studylife.example.test";

    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "sld-tests-" + Guid.NewGuid());
    private readonly Dictionary<string, string?> _overrides;

    public PortalFactory(Dictionary<string, string?>? overrides = null) => _overrides = overrides ?? [];

    public FakeStudyLife StudyLife { get; } = new();

    public KeyStore KeyStore => Services.GetRequiredService<KeyStore>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["StudyLifeDevelopers:SharedSecret"] = SharedSecret,
            ["DataDir"] = _dataDir,
            ["StudyLife:BaseUrl"] = "https://studylife-web.internal.test",
            ["PortalAuth:PublicUrl"] = "https://localhost",
            ["PortalAuth:StudyLifePublicUrl"] = StudyLifePublicUrl,
            ["HTTP_PORTS"] = PublicPort.ToString(CultureInfo.InvariantCulture),
            ["InternalApi:Port"] = InternalPort.ToString(CultureInfo.InvariantCulture),
            ["AllowedHosts"] = "localhost;alt.localhost",
        };
        foreach (var (key, value) in _overrides)
            settings[key] = value;
        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = StudyLife));
            services.AddSingleton<IStartupFilter, LocalPortStartupFilter>();
        });
    }

    /// <summary>A browser-like client: https (the session cookie is Secure), cookies kept,
    /// redirects not followed so every hop can be asserted.</summary>
    public HttpClient CreateBrowser(string host = "localhost") => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri($"https://{host}"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    /// <summary>Runs the full sign-in round trip as <paramref name="userId"/> and returns the
    /// response of the callback.</summary>
    public async Task<HttpResponseMessage> SignInAsync(HttpClient browser, int? userId = null, string returnUrl = "/")
    {
        KeyStore.SetKey(StudyLife.DeveloperKey);
        var login = await browser.GetAsync(new Uri($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}", UriKind.Relative));
        if (login.StatusCode != HttpStatusCode.Redirect)
            return login;
        var consent = ParseQuery(login.Headers.Location!);
        var assertion = StudyLife.IssueAssertion(userId ?? StudyLife.PairedUserId, new PortalAuthOptions().ClientId, consent["code_challenge"]);
        return await browser.GetAsync(new Uri(
            $"/auth/callback?assertion={Uri.EscapeDataString(assertion)}&state={Uri.EscapeDataString(consent["state"])}", UriKind.Relative));
    }

    public static Dictionary<string, string> ParseQuery(Uri uri) =>
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.Ordinal);

    /// <summary>The antiforgery token rendered into the sign-out form of an authenticated page.</summary>
    public static async Task<string> AntiforgeryTokenFromAsync(HttpResponseMessage page)
    {
        var html = await page.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "no antiforgery token on the page");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_dataDir))
        {
            try { Directory.Delete(_dataDir, recursive: true); }
            catch (IOException) { /* best effort: a handle may still be closing */ }
        }
    }

    private sealed class LocalPortStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.LocalPort = context.Request.Headers.TryGetValue(LocalPortHeader, out var port)
                    ? int.Parse(port.ToString(), CultureInfo.InvariantCulture)
                    : PublicPort;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
