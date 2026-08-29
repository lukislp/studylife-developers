using System.Text.Json.Serialization;
using StudyLifeDevelopers.Components;
using StudyLifeDevelopers.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<KeyStore>();
builder.Services.AddHttpClient<StudyLifeApiClient>((sp, http) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["StudyLife:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl)) http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
})
.ConfigurePrimaryHttpMessageHandler(sp => StudyLifeCaTrust.CreateHandler(sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Authenticated by StudyLifeDevelopers:SharedSecret - the studylife side of this pair (see
// DeveloperProxyClient.RegisterKeyAsync/RevokeKeyAsync there) never impersonates a specific
// user; this portal is single-tenant (one deployment paired to exactly one studylife instance),
// so the received key is simply THE key, see KeyStore.
var developerSharedSecret = app.Configuration["StudyLifeDevelopers:SharedSecret"];

bool HasValidSharedSecret(HttpRequest request) =>
    !string.IsNullOrEmpty(developerSharedSecret)
    && request.Headers.TryGetValue("X-StudyLife-Shared-Secret", out var provided)
    && provided == developerSharedSecret;

app.MapPost("/internal/register-key", (RegisterKeyRequest body, HttpRequest request, KeyStore keyStore) =>
{
    if (!HasValidSharedSecret(request)) return Results.Unauthorized();
    keyStore.SetKey(body.ApiKey);
    return Results.Ok();
});

app.MapPost("/internal/revoke-key", (HttpRequest request, KeyStore keyStore) =>
{
    if (!HasValidSharedSecret(request)) return Results.Unauthorized();
    keyStore.Clear();
    return Results.Ok();
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

/// <summary>Body of POST /internal/register-key - matches DeveloperProxyClient.RegisterKeyAsync's
/// anonymous-object payload shape exactly (snake_case, plain camelCase-unaware JSON, not ASP.NET
/// Core's own web-default naming policy). UserId is accepted for shape-compatibility but
/// otherwise unused: this portal is single-tenant, there is only ever one key (see KeyStore).</summary>
internal sealed record RegisterKeyRequest(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("api_key")] string ApiKey);

/// <summary>Exposes the top-level-statement-generated Program class to
/// Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory&lt;Program&gt; in the test project.</summary>
public partial class Program;
