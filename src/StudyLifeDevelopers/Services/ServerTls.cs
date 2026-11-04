using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace StudyLifeDevelopers.Services;

/// <summary>
/// HTTPS listener for the StudyLife -> studylife-developers hop. StudyLife's
/// DeveloperProxyClient sends the shared secret (X-StudyLife-Shared-Secret) and the user's API
/// key with /internal/register-key, so the hop has to be encrypted (OpenSSF
/// crypto_used_network, StudyLife's docs/SECURITY-ASSURANCE.md section 8.3). StudyLife validates
/// the certificate against its own internal CA, host name included (its AddonTls:* settings),
/// so the certificate is the cert-manager one from k8s/07-tls-certificate.yaml.
///
/// With ServerTls:CertPath and ServerTls:KeyPath set (PEM certificate chain and key), Kestrel
/// listens on the plain HTTP port(s) from ASPNETCORE_HTTP_PORTS as before (kubelet probes and the
/// gateway route) and additionally on ServerTls:Port (default 8443) over HTTPS, TLS 1.2 minimum.
/// Only one of the two paths set, or a pair that cannot be loaded, stops the start: the service
/// never runs without the TLS listener it was asked for. Neither set (the default) is the
/// explicit opt-out to plain HTTP only.
/// </summary>
public static class ServerTls
{
    public const string SectionName = "ServerTls";
    public const int DefaultPort = 8443;

    /// <summary>Wires the HTTPS listener into <paramref name="builder"/> when configured.
    /// Returns false (and changes nothing) when TLS is not configured.</summary>
    public static bool Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var section = builder.Configuration.GetSection(SectionName);
        var certPath = section["CertPath"];
        var keyPath = section["KeyPath"];
        if (string.IsNullOrWhiteSpace(certPath) && string.IsNullOrWhiteSpace(keyPath))
            return false;
        if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(keyPath))
            throw new InvalidOperationException(
                $"{SectionName}:CertPath and {SectionName}:KeyPath must be set together.");

        var tlsPort = section.GetValue<int?>("Port") ?? DefaultPort;
        var reloadInterval = TimeSpan.FromSeconds(section.GetValue<double?>("ReloadIntervalSeconds") ?? 300);
        // Loads the pair now, so a missing or broken file fails the start.
        var certificate = new ReloadingCertificate(certPath.Trim(), keyPath.Trim(), reloadInterval, TimeProvider.System);
        builder.Services.AddSingleton(certificate);
        var httpPorts = ParseHttpPorts(builder.Configuration["HTTP_PORTS"]);

        // One explicit Listen() replaces the ASPNETCORE_HTTP_PORTS endpoints entirely, so the
        // plain port(s) are listed again here, otherwise they would disappear.
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var port in httpPorts)
                kestrel.ListenAnyIP(port);
            kestrel.ListenAnyIP(tlsPort, listen => listen.UseHttps(CreateHttpsOptions(certificate)));
        });
        return true;
    }

    public static HttpsConnectionAdapterOptions CreateHttpsOptions(ReloadingCertificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return new HttpsConnectionAdapterOptions
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            // Asked on every handshake; ReloadingCertificate re-reads renewed files at most
            // once per reload interval.
            ServerCertificateSelector = (_, _) => certificate.Current,
        };
    }

    /// <summary>ASPNETCORE_HTTP_PORTS ("8080", "8080;8081"); 8080 when unset (the Dockerfile's
    /// value).</summary>
    public static IReadOnlyList<int> ParseHttpPorts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [8080];
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.Parse(p, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
    }
}

/// <summary>
/// The server certificate from a PEM certificate/key pair, re-read when the files change.
/// cert-manager rewrites the mounted Secret well before expiry; Kestrel asks for the certificate
/// on every handshake, so a renewed pair is served without a restart. A pair that cannot be
/// loaded during a renewal keeps the previous certificate (logged), a pair that cannot be loaded
/// at startup throws.
/// </summary>
public sealed class ReloadingCertificate
{
    private readonly string _certPath;
    private readonly string _keyPath;
    private readonly TimeSpan _checkInterval;
    private readonly TimeProvider _time;
    private readonly Lock _gate = new();
    private X509Certificate2 _current;
    private (DateTime, long, DateTime, long) _fingerprint;
    private DateTimeOffset _nextCheck;

    public ReloadingCertificate(string certPath, string keyPath, TimeSpan checkInterval, TimeProvider time)
    {
        _certPath = certPath;
        _keyPath = keyPath;
        _checkInterval = checkInterval;
        _time = time;
        _fingerprint = Fingerprint();
        _current = Load(certPath, keyPath);
        _nextCheck = time.GetUtcNow() + checkInterval;
    }

    /// <summary>Number of successful reloads after the initial load (for tests and logs).</summary>
    public int ReloadCount { get; private set; }

    public X509Certificate2 Current
    {
        get
        {
            lock (_gate)
            {
                var now = _time.GetUtcNow();
                if (now < _nextCheck)
                    return _current;
                _nextCheck = now + _checkInterval;
                try
                {
                    var fingerprint = Fingerprint();
                    if (fingerprint != _fingerprint)
                    {
                        _current = Load(_certPath, _keyPath);
                        _fingerprint = fingerprint;
                        ReloadCount++;
                    }
                }
                catch (Exception ex) when (ex is IOException or CryptographicException or ArgumentException or UnauthorizedAccessException)
                {
                    // Mid-update or broken renewal: keep serving the previous certificate and
                    // try again after the next interval.
                    Console.Error.WriteLine($"ServerTls: certificate reload failed, keeping the previous certificate: {ex.Message}");
                }
                return _current;
            }
        }
    }

    private (DateTime, long, DateTime, long) Fingerprint()
    {
        // A Kubernetes Secret volume swaps a symlink on update; FileInfo follows it.
        var cert = new FileInfo(_certPath);
        var key = new FileInfo(_keyPath);
        if (!cert.Exists || !key.Exists)
            throw new FileNotFoundException($"TLS certificate or key not found: {_certPath}, {_keyPath}");
        return (cert.LastWriteTimeUtc, cert.Length, key.LastWriteTimeUtc, key.Length);
    }

    private static X509Certificate2 Load(string certPath, string keyPath)
    {
        using var pem = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        // A PEM-loaded key is ephemeral; SslStream on Windows needs a persisted one, so the pair
        // goes through PKCS#12 once. On Linux this is a harmless copy.
        return X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), password: null);
    }
}
