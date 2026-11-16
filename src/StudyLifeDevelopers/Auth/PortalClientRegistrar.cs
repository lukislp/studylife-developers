using Microsoft.Extensions.Options;
using StudyLifeDevelopers.Services;

namespace StudyLifeDevelopers.Auth;

/// <summary>
/// Makes sure the portal's own sign-in client exists on the paired StudyAtrium instance, owned by
/// the paired account, with exactly the expected redirect URI and the smallest possible scope
/// (Auth.Whoami - no study data). Runs before every sign-in, through the developer key the
/// portal already holds, so a deleted or edited registration heals itself instead of locking
/// the operator out. The ownership check matters: client ids are unique per instance, and a
/// registration of the same id by another account would hand that account control over the
/// consent screen, so it is refused rather than used.
/// </summary>
public sealed class PortalClientRegistrar(StudyLifeApiClient api, IOptions<PortalAuthOptions> options)
{
    public const string SignInScope = "Auth.Whoami";

    public enum Outcome { Ready, ClientIdTaken, Failed }

    public async Task<Outcome> EnsureRegisteredAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        var redirectUri = settings.CallbackUri.AbsoluteUri;
        try
        {
            // Owner-filtered by StudyAtrium: only the paired account's own registrations.
            var own = await api.GetClientsAsync(ct);
            var existing = own.FirstOrDefault(c => string.Equals(c.ClientId, settings.ClientId, StringComparison.Ordinal));
            if (existing is null)
            {
                var (created, error, _) = await api.CreateClientAsync(new CreateDeveloperClientRequestDto
                {
                    ClientId = settings.ClientId,
                    Name = DisplayName,
                    Description = Description,
                    AllowedRedirectUris = [redirectUri],
                    RequestedScopes = [SignInScope],
                }, ct);
                if (created) return Outcome.Ready;
                // Not in our own list, yet StudyAtrium says the id exists: another account owns it
                // (DeveloperClientService: "ClientId '...' is already taken.").
                return error?.Contains("already taken", StringComparison.OrdinalIgnoreCase) == true
                    ? Outcome.ClientIdTaken
                    : Outcome.Failed;
            }

            if (IsAsExpected(existing, redirectUri))
                return Outcome.Ready;

            var (updated, _, _) = await api.UpdateClientAsync(settings.ClientId, new UpdateDeveloperClientRequestDto
            {
                Name = DisplayName,
                Description = Description,
                AllowedRedirectUris = [redirectUri],
                RequestedScopes = [SignInScope],
            }, ct);
            return updated ? Outcome.Ready : Outcome.Failed;
        }
        catch (HttpRequestException)
        {
            return Outcome.Failed;
        }
    }

    public const string DisplayName = "StudyAtrium Developers sign-in";
    public const string Description = "Signs you in to your own StudyAtrium developer portal. Grants no access to your study data.";

    private static bool IsAsExpected(DeveloperClientDto client, string redirectUri) =>
        client.AllowedRedirectUris.Count == 1
        && string.Equals(client.AllowedRedirectUris[0], redirectUri, StringComparison.Ordinal)
        && client.RequestedScopes.Count == 1
        && string.Equals(client.RequestedScopes[0], SignInScope, StringComparison.Ordinal);
}
