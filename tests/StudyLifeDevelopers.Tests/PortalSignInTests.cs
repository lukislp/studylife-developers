using System.Net;
using StudyLifeDevelopers.Auth;
using StudyLifeDevelopers.Tests.Infrastructure;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// The portal UI and its Blazor circuit are behind StudyAtrium sign-in, and only the paired
/// account gets in (audit 2026-11-16, finding M6). End to end on TestServer against
/// <see cref="FakeStudyLife"/>.
/// </summary>
public sealed class PortalSignInTests : IDisposable
{
    private readonly PortalFactory _factory = new();
    private readonly HttpClient _browser;

    public PortalSignInTests() => _browser = _factory.CreateBrowser();

    // ── Unauthenticated access is refused ─────────────────────────────────────────────────

    [Theory]
    [InlineData("/")]
    [InlineData("/addons/new")]
    [InlineData("/addons/some-addon/edit")]
    public async Task Pages_redirect_an_anonymous_browser_to_sign_in(string path)
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);

        var response = await _browser.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login", response.Headers.Location!.AbsolutePath);
        Assert.Equal(path, PortalFactory.ParseQuery(response.Headers.Location)["returnUrl"]);
    }

    [Fact]
    public async Task The_blazor_circuit_cannot_be_opened_anonymously()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/_blazor/negotiate?negotiateVersion=1");

        var response = await _browser.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized, $"got {response.StatusCode}");
    }

    [Fact]
    public async Task Nothing_of_the_paired_account_is_rendered_before_sign_in()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        _factory.StudyLife.OwnClients["secret-addon"] = new() { ClientId = "secret-addon", Name = "Secret Add-on" };

        var response = await _browser.GetAsync(new Uri("/", UriKind.Relative));

        Assert.DoesNotContain("Secret Add-on", await response.Content.ReadAsStringAsync());
    }

    // ── Happy path ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_registers_the_minimal_sign_in_client_and_sends_the_browser_to_StudyAtrium_consent()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);

        var response = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal($"{PortalFactory.StudyLifePublicUrl}/connect/client/studylife-developers-portal", location.GetLeftPart(UriPartial.Path));
        var query = PortalFactory.ParseQuery(location);
        Assert.Equal("https://localhost/auth/callback", query["redirect_uri"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(43, query["code_challenge"].Length);
        Assert.False(string.IsNullOrEmpty(query["state"]));

        var client = _factory.StudyLife.OwnClients["studylife-developers-portal"];
        Assert.Equal(["https://localhost/auth/callback"], client.AllowedRedirectUris);
        Assert.Equal(["Auth.Whoami"], client.RequestedScopes);
    }

    [Fact]
    public async Task The_paired_account_signs_in_and_gets_a_hardened_session_cookie()
    {
        var callback = await _factory.SignInAsync(_browser, returnUrl: "/addons/new");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/addons/new", callback.Headers.Location!.OriginalString);
        var cookie = callback.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(PortalSession.CookieName + "=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);

        var home = await _browser.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("Your add-ons", await home.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_assertion_is_redeemed_with_the_pkce_verifier_of_this_attempt()
    {
        await _factory.SignInAsync(_browser);

        var exchange = Assert.Single(_factory.StudyLife.Exchanges);
        Assert.Equal("studylife-developers-portal", exchange.ClientId);
        Assert.Equal(43, exchange.CodeVerifier!.Length);
    }

    [Fact]
    public async Task An_edited_sign_in_client_is_restored_on_the_next_login()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        _factory.StudyLife.OwnClients["studylife-developers-portal"] = new()
        {
            ClientId = "studylife-developers-portal",
            AllowedRedirectUris = ["https://attacker.example/cb"],
            RequestedScopes = ["Notes.GetAll"],
        };

        await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));

        var client = _factory.StudyLife.OwnClients["studylife-developers-portal"];
        Assert.Equal(["https://localhost/auth/callback"], client.AllowedRedirectUris);
        Assert.Equal(["Auth.Whoami"], client.RequestedScopes);
    }

    // ── Refused sign-ins ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Another_StudyAtrium_account_is_refused()
    {
        var callback = await _factory.SignInAsync(_browser, userId: 99);

        Assert.Equal("/auth/denied?reason=wrong-account", callback.Headers.Location!.OriginalString);
        Assert.DoesNotContain(callback.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            c => c.StartsWith(PortalSession.CookieName + "=", StringComparison.Ordinal) && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.Ordinal));
        var home = await _browser.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
    }

    [Fact]
    public async Task A_callback_with_a_foreign_state_is_refused()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));
        var consent = PortalFactory.ParseQuery(login.Headers.Location!);
        var assertion = _factory.StudyLife.IssueAssertion(7, "studylife-developers-portal", consent["code_challenge"]);

        var callback = await _browser.GetAsync(new Uri($"/auth/callback?assertion={assertion}&state=forged-state", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=invalid-request", callback.Headers.Location!.OriginalString);
        Assert.Empty(_factory.StudyLife.Exchanges);
    }

    [Fact]
    public async Task A_callback_without_a_sign_in_started_in_this_browser_is_refused()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        var assertion = _factory.StudyLife.IssueAssertion(7, "studylife-developers-portal", Pkce.ChallengeFor(Pkce.NewVerifier()));

        var callback = await _browser.GetAsync(new Uri($"/auth/callback?assertion={assertion}&state=anything", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=invalid-request", callback.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_callback_is_single_use()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));
        var consent = PortalFactory.ParseQuery(login.Headers.Location!);
        var assertion = _factory.StudyLife.IssueAssertion(7, "studylife-developers-portal", consent["code_challenge"]);
        var url = new Uri($"/auth/callback?assertion={assertion}&state={Uri.EscapeDataString(consent["state"])}", UriKind.Relative);
        Assert.Equal("/", (await _browser.GetAsync(url)).Headers.Location!.OriginalString);

        using var replayBrowser = _factory.CreateBrowser();
        var replay = await replayBrowser.GetAsync(url);

        Assert.Equal("/auth/denied?reason=invalid-request", replay.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task An_assertion_StudyAtrium_rejects_is_refused()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));
        var consent = PortalFactory.ParseQuery(login.Headers.Location!);

        var callback = await _browser.GetAsync(new Uri($"/auth/callback?assertion=made-up&state={Uri.EscapeDataString(consent["state"])}", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=rejected", callback.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Without_a_pairing_nobody_can_sign_in()
    {
        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=not-paired", login.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_sign_in_client_id_owned_by_another_account_is_never_used()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        _factory.StudyLife.ClientIdsOwnedByOthers["studylife-developers-portal"] = 0;

        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=client-id-taken", login.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task An_unreachable_StudyAtrium_fails_closed()
    {
        _factory.KeyStore.SetKey(_factory.StudyLife.DeveloperKey);
        _factory.StudyLife.Unreachable = true;

        var login = await _browser.GetAsync(new Uri("/auth/login", UriKind.Relative));

        Assert.Equal("/auth/denied?reason=unavailable", login.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("//evil.example/x")]
    [InlineData("/\\evil.example")]
    [InlineData("https://evil.example/")]
    public async Task The_return_url_cannot_leave_the_portal(string returnUrl)
    {
        var callback = await _factory.SignInAsync(_browser, returnUrl: returnUrl);

        Assert.Equal("/", callback.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_login_on_another_allowed_host_name_moves_to_the_canonical_one_first()
    {
        using var browser = _factory.CreateBrowser("alt.localhost");

        var response = await browser.GetAsync(new Uri("/auth/login?returnUrl=%2Faddons%2Fnew", UriKind.Relative));

        Assert.Equal("https://localhost/auth/login?returnUrl=%2Faddons%2Fnew", response.Headers.Location!.AbsoluteUri);
    }

    [Fact]
    public async Task Sign_in_attempts_are_rate_limited()
    {
        using var factory = new PortalFactory(new() { ["PortalAuth:SignInPermitsPerMinute"] = "2" });
        using var browser = factory.CreateBrowser();
        factory.KeyStore.SetKey(factory.StudyLife.DeveloperKey);

        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync(new Uri("/auth/login", UriKind.Relative))).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await browser.GetAsync(new Uri("/auth/login", UriKind.Relative))).StatusCode);
    }

    // ── Session lifecycle ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoking_the_pairing_ends_existing_sessions()
    {
        await _factory.SignInAsync(_browser);
        Assert.Equal(HttpStatusCode.OK, (await _browser.GetAsync(new Uri("/", UriKind.Relative))).StatusCode);

        _factory.KeyStore.Clear();

        var response = await _browser.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Pairing_again_with_a_new_key_ends_existing_sessions()
    {
        await _factory.SignInAsync(_browser);

        _factory.KeyStore.SetKey("a-new-developer-key");

        var response = await _browser.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Sign_out_requires_the_antiforgery_token()
    {
        await _factory.SignInAsync(_browser);

        var response = await _browser.PostAsync(new Uri("/auth/logout", UriKind.Relative), new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _browser.GetAsync(new Uri("/", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task Sign_out_with_the_token_ends_the_session()
    {
        await _factory.SignInAsync(_browser);
        var token = await PortalFactory.AntiforgeryTokenFromAsync(await _browser.GetAsync(new Uri("/", UriKind.Relative)));

        var response = await _browser.PostAsync(new Uri("/auth/logout", UriKind.Relative),
            new FormUrlEncodedContent([new("__RequestVerificationToken", token)]));

        Assert.Equal("/auth/signed-out", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await _browser.GetAsync(new Uri("/", UriKind.Relative))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _browser.GetAsync(new Uri("/auth/signed-out", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task A_cross_origin_post_is_refused_even_with_a_session()
    {
        await _factory.SignInAsync(_browser);
        var token = await PortalFactory.AntiforgeryTokenFromAsync(await _browser.GetAsync(new Uri("/", UriKind.Relative)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout")
        {
            Content = new FormUrlEncodedContent([new("__RequestVerificationToken", token)]),
        };
        request.Headers.Add("Origin", "https://sibling.localhost");

        var response = await _browser.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_denied_page_is_reachable_anonymously()
    {
        var response = await _browser.GetAsync(new Uri("/auth/denied?reason=wrong-account", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Wrong StudyAtrium account", await response.Content.ReadAsStringAsync());
    }

    public void Dispose()
    {
        _browser.Dispose();
        _factory.Dispose();
    }
}
