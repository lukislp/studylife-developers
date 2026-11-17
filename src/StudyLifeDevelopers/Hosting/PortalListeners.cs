namespace StudyLifeDevelopers.Hosting;

/// <summary>
/// Binds Kestrel to the <see cref="ListenerLayout"/>: the public port(s) over plain HTTP and the
/// internal port, over HTTPS when ServerTls is configured. One explicit Listen() replaces the
/// ASPNETCORE_HTTP_PORTS endpoints entirely, so the public ports are listed here again.
/// </summary>
public static class PortalListeners
{
    public static ListenerLayout Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // Loads the certificate pair now, so a missing or broken file fails the start.
        var certificate = ServerTls.LoadCertificate(builder.Configuration);
        if (certificate is not null)
            builder.Services.AddSingleton(certificate);

        var layout = ListenerLayout.From(builder.Configuration);
        builder.Services.AddSingleton(layout);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var port in layout.PublicPorts)
                kestrel.ListenAnyIP(port);
            kestrel.ListenAnyIP(layout.InternalPort, listen =>
            {
                if (certificate is not null)
                    listen.UseHttps(ServerTls.CreateHttpsOptions(certificate));
            });
        });
        return layout;
    }
}
