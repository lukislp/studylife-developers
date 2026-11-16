using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StudyLifeDevelopers.Hosting;

namespace StudyLifeDevelopers.Tests;

/// <summary>
/// ListenerSeparationMiddleware on real Kestrel sockets: the decision is taken on the local port
/// the connection arrived on, so this is the one place it can be proven without the TestServer
/// stand-in that PortalFactory uses.
/// </summary>
public sealed class ListenerSeparationTests
{
    [Fact]
    public async Task Internal_endpoints_exist_only_on_the_internal_port_and_the_ui_only_on_the_public_one()
    {
        var publicPort = FreePort();
        var internalPort = FreePort();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HTTP_PORTS"] = publicPort.ToString(CultureInfo.InvariantCulture),
            ["InternalApi:Port"] = internalPort.ToString(CultureInfo.InvariantCulture),
        });
        PortalListeners.Configure(builder);
        await using var app = builder.Build();
        app.UseMiddleware<ListenerSeparationMiddleware>();
        app.MapGet("/health", () => "ok");
        app.MapPost("/internal/register-key", () => "internal");
        app.MapGet("/", () => "ui");
        await app.StartAsync();

        using var http = new HttpClient();
        async Task<HttpStatusCode> Status(HttpMethod method, int port, string path)
        {
            using var request = new HttpRequestMessage(method, new Uri($"http://localhost:{port}{path}"));
            using var response = await http.SendAsync(request);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.OK, await Status(HttpMethod.Get, publicPort, "/"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(HttpMethod.Post, publicPort, "/internal/register-key"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(HttpMethod.Post, publicPort, "/INTERNAL/register-key"));
        Assert.Equal(HttpStatusCode.OK, await Status(HttpMethod.Get, publicPort, "/health"));

        Assert.Equal(HttpStatusCode.OK, await Status(HttpMethod.Post, internalPort, "/internal/register-key"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(HttpMethod.Get, internalPort, "/"));
        Assert.Equal(HttpStatusCode.OK, await Status(HttpMethod.Get, internalPort, "/health"));

        // A spoofed Host header naming the internal port changes nothing: only the socket counts.
        using var spoofed = new HttpRequestMessage(HttpMethod.Post, new Uri($"http://localhost:{publicPort}/internal/register-key"));
        spoofed.Headers.Host = $"localhost:{internalPort}";
        using var spoofedResponse = await http.SendAsync(spoofed);
        Assert.Equal(HttpStatusCode.NotFound, spoofedResponse.StatusCode);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
