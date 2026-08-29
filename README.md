# StudyLife Developers

A developer portal for registering [StudyLife](https://github.com/lukislp/studylife) add-ons —
paired to exactly one StudyLife instance, like every other satellite in this ecosystem (Tray,
Webhooks, HACS, MCP). Register a new add-on here, pick which scopes it needs, get a `ClientId`
and a redirect URI to build into your app, then submit it to
[studylife-marketplace](https://github.com/lukislp/studylife-marketplace) once it's ready for
other people to install.

## How it works

1. **Enable the connection**: on your paired StudyLife instance's Setup page, enable the
   "studylife-developers connection" card. StudyLife generates a key and registers it with this
   portal automatically — you never see or copy it (same pattern as the studylife-ai card).
2. **Register an add-on**: open this portal, click "Register new add-on", give it a name,
   description, redirect URI(s), and the scopes it needs.
3. **Build your add-on**: it authenticates end users via StudyLife's generic connect flow —
   `POST /api/auth/connect` (session-required, on the user's own StudyLife instance) with your
   `ClientId`, then `POST /api/auth/assertion-exchange` server-to-server to redeem the resulting
   assertion for a real API key. See the studylife repo's `AuthController.10.OAuthClients.cs` for
   the exact request/response shapes.
4. **Publish it**: once it works end to end against your own account, submit a manifest to
   [studylife-marketplace](https://github.com/lukislp/studylife-marketplace) via pull request so
   other StudyLife instances can discover and install it.

## What this portal is NOT

It doesn't host or run your add-on's code — it only manages the *registration* (name, scopes,
redirect URIs) on your paired StudyLife instance. Your add-on runs wherever you deploy it.

## Configuration

| Env var | Description |
| --- | --- |
| `StudyLife__BaseUrl` | Base URL of the paired StudyLife instance's API |
| `StudyLifeDevelopers__SharedSecret` | Must match the studylife repo's `StudyLifeDevelopers:SharedSecret` exactly — authenticates every `/internal/*` call as genuinely coming from StudyLife |
| `DataDir` | Directory for the single-file key store (default `data`) |

## Scopes

The scopes an add-on may request are a fixed, curated list — see
[`Services/ScopeCatalog.cs`](src/StudyLifeDevelopers/Services/ScopeCatalog.cs). It mirrors
`ApiKeyScopes.PubliclyGrantable` in the studylife repo and `schema/known-scopes.json` in
studylife-marketplace; all three are kept in sync by hand.
