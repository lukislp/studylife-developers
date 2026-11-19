using System.Net;
using System.Text.RegularExpressions;
using StudyLifeDevelopers.Tests.Infrastructure;

namespace StudyLifeDevelopers.Tests;

/// <summary>GDPR audit 2026-11-19, S11: the portal must not load fonts (or any other
/// resource) from a third-party host, and the vendored fonts must be served with the right
/// content type.</summary>
public sealed partial class SelfHostedFontsTests : IDisposable
{
    private readonly PortalFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [GeneratedRegex(@"url\(\s*['""]?([^'"")\s]+)['""]?\s*\)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CssUrl();

    private async Task<string> GetStringAsync(string path)
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData("/css/base.css")]
    [InlineData("/css/shared.css")]
    [InlineData("/css/setup.css")]
    [InlineData("/css/responsive.css")]
    [InlineData("/css/app.css")]
    public async Task Stylesheets_reference_no_external_host(string path)
    {
        var css = await GetStringAsync(path);

        Assert.DoesNotContain("fonts.googleapis.com", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fonts.gstatic.com", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", css, StringComparison.OrdinalIgnoreCase);
        foreach (Match m in CssUrl().Matches(css))
            Assert.DoesNotMatch(@"^(https?:)?//", m.Groups[1].Value);
    }

    [Fact]
    public async Task The_page_loads_only_local_stylesheets_and_scripts()
    {
        using var client = _factory.CreateBrowser();
        // The signed-out portal redirects to sign-in; the layout shell is the same everywhere.
        var response = await client.GetAsync(new Uri("/auth/signed-out", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("fonts.googleapis.com", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"(?:src|href)=""(?:https?:)?//", html);
    }

    [Fact]
    public async Task Every_font_the_stylesheet_declares_is_served_as_woff2()
    {
        var css = await GetStringAsync("/css/base.css");
        var fonts = CssUrl().Matches(css).Select(m => m.Groups[1].Value).Where(u => u.EndsWith(".woff2", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(fonts);

        using var client = _factory.CreateBrowser();
        foreach (var url in fonts)
        {
            // base.css lives in /css/, the declared urls are relative to it.
            var resolved = new Uri(new Uri("https://localhost/css/base.css"), url);
            var response = await client.GetAsync(resolved);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("font/woff2", response.Content.Headers.ContentType?.MediaType);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal("wOF2", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }

    [Fact]
    public async Task The_font_license_notice_ships_with_the_fonts()
    {
        var license = await GetStringAsync("/fonts/OFL.txt");

        Assert.Contains("SIL OPEN FONT LICENSE", license, StringComparison.Ordinal);
        Assert.Contains("DM Sans Project Authors", license, StringComparison.Ordinal);
        Assert.Contains("DM Mono Project Authors", license, StringComparison.Ordinal);
    }
}
