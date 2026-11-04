using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// ServerTls: real Kestrel listeners and real TLS handshakes against a throwaway private CA,
/// validated the way StudyLife's PrivateCaCertificateValidator does it (custom root, host name
/// checked). WebApplicationFactory's TestServer has no network stack, so these start Kestrel on
/// OS-assigned ports instead.
/// </summary>
public sealed class ServerTlsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("serverTls").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Configure_without_settings_changes_nothing()
    {
        var builder = WebApplication.CreateBuilder();
        Assert.False(ServerTls.Configure(builder));
    }

    [Fact]
    public void Configure_rejects_a_half_configured_pair()
    {
        var builder = Builder(new Dictionary<string, string?> { ["ServerTls:CertPath"] = "/etc/tls/tls.crt" });
        var ex = Assert.Throws<InvalidOperationException>(() => ServerTls.Configure(builder));
        Assert.Contains("set together", ex.Message);
    }

    [Fact]
    public void Configure_fails_on_a_missing_certificate()
    {
        var builder = Builder(new Dictionary<string, string?>
        {
            ["ServerTls:CertPath"] = Path.Combine(_dir, "missing.crt"),
            ["ServerTls:KeyPath"] = Path.Combine(_dir, "missing.key"),
        });
        Assert.Throws<FileNotFoundException>(() => ServerTls.Configure(builder));
    }

    [Theory]
    [InlineData(null, new[] { 8080 })]
    [InlineData("8080", new[] { 8080 })]
    [InlineData("8080;8081", new[] { 8080, 8081 })]
    public void ParseHttpPorts_reads_aspnetcore_http_ports(string? value, int[] expected) =>
        Assert.Equal(expected, ServerTls.ParseHttpPorts(value));

    [Fact]
    public async Task Serves_https_trusted_by_the_private_ca_and_keeps_plain_http()
    {
        using var ca = CreateCa("test-ca");
        var (certPath, keyPath) = WriteLeaf(ca, "leaf");
        await using var app = await StartAsync(certPath, keyPath);
        var (httpPort, httpsPort) = Ports(app);

        using (var client = new HttpClient(TrustingHandler(ca)))
        {
            var response = await client.GetAsync(new Uri($"https://localhost:{httpsPort}/health"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // The plain port (kubelet probes, gateway) is still served.
        using (var plain = new HttpClient())
        {
            var response = await plain.GetAsync(new Uri($"http://localhost:{httpPort}/health"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Plain HTTP on the TLS port does not get an answer.
        using (var plain = new HttpClient())
        {
            await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
                plain.GetAsync(new Uri($"http://localhost:{httpsPort}/health")));
        }
    }

    [Fact]
    public async Task A_client_trusting_another_ca_is_refused()
    {
        using var ca = CreateCa("test-ca");
        using var other = CreateCa("other-ca");
        var (certPath, keyPath) = WriteLeaf(ca, "leaf");
        await using var app = await StartAsync(certPath, keyPath);
        var (_, httpsPort) = Ports(app);

        using var client = new HttpClient(TrustingHandler(other));
        await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
            client.GetAsync(new Uri($"https://localhost:{httpsPort}/health")));
    }

    [Fact]
    public void Only_tls_1_2_and_1_3_are_offered()
    {
        using var ca = CreateCa("test-ca");
        var (certPath, keyPath) = WriteLeaf(ca, "leaf");
        var options = ServerTls.CreateHttpsOptions(
            new ReloadingCertificate(certPath, keyPath, TimeSpan.FromMinutes(5), TimeProvider.System));
        Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, options.SslProtocols);
    }

    [Fact]
    public void Renewed_files_are_picked_up_after_the_interval_and_broken_ones_are_ignored()
    {
        using var ca = CreateCa("test-ca");
        var (certPath, keyPath) = WriteLeaf(ca, "leaf");
        var time = new ManualTime();
        var certificate = new ReloadingCertificate(certPath, keyPath, TimeSpan.FromMinutes(5), time);
        var first = certificate.Current.SerialNumber;

        WriteLeaf(ca, "leaf");
        File.SetLastWriteTimeUtc(certPath, DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(first, certificate.Current.SerialNumber); // before the interval: cached

        time.Advance(TimeSpan.FromMinutes(6));
        var renewed = certificate.Current.SerialNumber;
        Assert.NotEqual(first, renewed);
        Assert.Equal(1, certificate.ReloadCount);

        File.WriteAllText(certPath, "half-written");
        time.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(renewed, certificate.Current.SerialNumber);
        Assert.Equal(1, certificate.ReloadCount);
    }

    private static WebApplicationBuilder Builder(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        return builder;
    }

    private static async Task<WebApplication> StartAsync(string certPath, string keyPath)
    {
        var builder = Builder(new Dictionary<string, string?>
        {
            ["ServerTls:CertPath"] = certPath,
            ["ServerTls:KeyPath"] = keyPath,
            ["ServerTls:Port"] = "0",
            ["HTTP_PORTS"] = "0",
        });
        Assert.True(ServerTls.Configure(builder));
        var app = builder.Build();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        await app.StartAsync();
        return app;
    }

    private static (int Http, int Https) Ports(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        int PortOf(string scheme) => new Uri(addresses.Single(a => a.StartsWith(scheme + "://", StringComparison.Ordinal))
            .Replace("[::]", "localhost", StringComparison.Ordinal).Replace("0.0.0.0", "localhost", StringComparison.Ordinal)).Port;
        return (PortOf("http"), PortOf("https"));
    }

    private static SocketsHttpHandler TrustingHandler(X509Certificate2 ca)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, _, errors) =>
        {
            if (cert is null || (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                return false;
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            var leaf = cert as X509Certificate2 ?? X509CertificateLoader.LoadCertificate(cert.GetRawCertData());
            return chain.Build(leaf);
        };
        return handler;
    }

    private static X509Certificate2 CreateCa(string name)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
    }

    private (string CertPath, string KeyPath) WriteLeaf(X509Certificate2 ca, string name)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        var serial = RandomNumberGenerator.GetBytes(12);
        serial[0] &= 0x7F;
        using var leaf = request.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-4), DateTimeOffset.UtcNow.AddHours(12), serial);
        var certPath = Path.Combine(_dir, name + ".crt");
        var keyPath = Path.Combine(_dir, name + ".key");
        File.WriteAllText(certPath, leaf.ExportCertificatePem());
        File.WriteAllText(keyPath, key.ExportPkcs8PrivateKeyPem());
        return (certPath, keyPath);
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
