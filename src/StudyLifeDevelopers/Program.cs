using StudyLifeDevelopers.Auth;
using StudyLifeDevelopers.Components;
using StudyLifeDevelopers.Hosting;
using StudyLifeDevelopers.Internal;
using StudyLifeDevelopers.Security;
using StudyLifeDevelopers.Services;

var builder = WebApplication.CreateBuilder(args);

// Public port(s) for the UI, a separate internal port for StudyAtrium's /internal/* calls
// (HTTPS when ServerTls is configured), see ListenerLayout.
PortalListeners.Configure(builder);

// Host header pinned to AllowedHosts (default "localhost", never "*" outside Development).
builder.Services.AddHostPinning(builder.Configuration, builder.Environment);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddStudyLifeClients();
builder.Services.AddInternalApi(builder.Configuration);
builder.Services.AddPortalSignIn(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Order matters: listener separation and the origin check run before anything else answers.
// Host filtering (HostPinning) is applied by the framework ahead of all of these.
app.UseMiddleware<ListenerSeparationMiddleware>();
app.UseMiddleware<SameOriginRequestMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapGet(ListenerSeparationMiddleware.HealthPath, () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapInternalApi();
app.MapPortalSignIn();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

/// <summary>Exposes the top-level-statement-generated Program class to
/// Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory&lt;Program&gt; in the test project.</summary>
public partial class Program;
