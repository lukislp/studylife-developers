using System.Security.Cryptography.X509Certificates;

namespace StudyLifeDevelopers.Services;

/// <summary>
/// StudyLife's own UseHttpsRedirection middleware forces HTTPS unconditionally for every
/// non-kube-probe request (see studylife's Program.cs) - reaching its cluster-internal Service
/// on the plain-HTTP port just gets redirected to :8443, so calling it at all means trusting its
/// internal cert-manager CA, which no public trust store knows about. Same requirement and same
/// public certificate studylife-ai/studylife-mcp already trust (see their own k8s/*-studylife-
/// ca.yaml, "StudyLife:CaCertPath" here mounts the identical ConfigMap content into THIS
/// namespace instead - a ConfigMap is namespace-scoped, can't be shared across namespaces
/// directly). If unset (e.g. local dev against a plain-http StudyLife), the default handler with
/// no custom validation is used, so this never breaks a non-TLS setup.
/// </summary>
public static class StudyLifeCaTrust
{
    public static HttpClientHandler CreateHandler(IConfiguration configuration)
    {
        var handler = new HttpClientHandler();
        var caCertPath = configuration["StudyLife:CaCertPath"];
        if (string.IsNullOrEmpty(caCertPath) || !File.Exists(caCertPath)) return handler;

        var caCert = X509CertificateLoader.LoadCertificateFromFile(caCertPath);
        handler.ServerCertificateCustomValidationCallback = (_, cert, chain, _) =>
        {
            if (cert is null || chain is null) return false;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Clear();
            chain.ChainPolicy.CustomTrustStore.Add(caCert);
            return chain.Build(cert);
        };
        return handler;
    }
}
