# StudyAtrium Developers

(formerly StudyLife)

[![CI](https://github.com/lukislp/studylife-developers/actions/workflows/ci.yml/badge.svg)](https://github.com/lukislp/studylife-developers/actions/workflows/ci.yml) [![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/lukislp/studylife-developers/badge)](https://scorecard.dev/viewer/?uri=github.com/lukislp/studylife-developers) [![CodeQL](https://github.com/lukislp/studylife-developers/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lukislp/studylife-developers/security/code-scanning)
[![Release](https://img.shields.io/github/v/release/lukislp/studylife-developers)](https://github.com/lukislp/studylife-developers/releases)
[![License: AGPL-3.0](https://img.shields.io/github/license/lukislp/studylife-developers)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

A developer portal for registering [StudyAtrium](https://github.com/lukislp/studylife) add-ons —
paired to exactly one StudyAtrium instance, like every other satellite in this ecosystem (Tray,
Webhooks, HACS, MCP). Register a new add-on here, pick which scopes it needs, get a `ClientId`
and a redirect URI to build into your app, then submit it to
[studylife-marketplace](https://github.com/lukislp/studylife-marketplace) once it's ready for
other people to install.

It is a small ASP.NET Core / Blazor Server app — one page listing your registered add-ons, one
registration form, a single-file key store, and two `/internal/*` endpoints its paired StudyAtrium
instance calls on a separate internal port. No database and no password of its own: you sign in
with your StudyAtrium account, and only the account the portal is paired with gets in (see
[Sign-in](#sign-in)).

## How it works

1. **Enable the connection**: on your paired StudyAtrium instance's Setup page, enable the
   "studylife-developers connection" card. StudyAtrium generates a key and registers it with this
   portal automatically — you never see or copy it (same pattern as the studylife-ai card).
2. **Register an add-on**: open this portal and sign in with the same StudyAtrium account, click
   "Register new add-on", give it a name,
   description, redirect URI(s), and the scopes it needs.
3. **Build your add-on**: it authenticates end users via StudyAtrium's generic connect flow —
   `POST /api/auth/connect` (session-required, on the user's own StudyAtrium instance) with your
   `ClientId`, then `POST /api/auth/assertion-exchange` server-to-server to redeem the resulting
   assertion for a real API key. See the studylife repo's `AuthController.10.OAuthClients.cs` for
   the exact request/response shapes.
4. **Publish it**: once it works end to end against your own account, submit a manifest to
   [studylife-marketplace](https://github.com/lukislp/studylife-marketplace) via pull request so
   other StudyAtrium instances can discover and install it.

## What this portal is NOT

It doesn't host or run your add-on's code — it only manages the *registration* (name, scopes,
redirect URIs) on your paired StudyAtrium instance. Your add-on runs wherever you deploy it.

## Quick start

You need a StudyAtrium instance to pair with: the portal talks to that instance's
`/api/developer/*` endpoints, and it only becomes usable once the instance has pushed its key
here (step 1 above). Until then the UI shows a "not connected" state instead of attempting any
request.

### Locally (.NET 10 SDK)

```bash
dotnet restore StudyLifeDevelopers.slnx
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyAtrium:BaseUrl" "https://studylife.example.com"
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyLifeDevelopers:SharedSecret" "<the same value your StudyAtrium instance has>"
dotnet user-secrets --project src/StudyLifeDevelopers set "PortalAuth:PublicUrl" "http://localhost:5080"
dotnet user-secrets --project src/StudyLifeDevelopers set "PortalAuth:StudyLifePublicUrl" "https://studylife.example.com"
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_HTTP_PORTS=5080 dotnet run --project src/StudyLifeDevelopers
```

Then open <http://localhost:5080>. Every setting works equally as an environment variable
(`StudyLife__BaseUrl`, `StudyLifeDevelopers__SharedSecret`, `PortalAuth__PublicUrl`, ...) or as an
edit to
`src/StudyLifeDevelopers/appsettings.json` — user secrets just keep the shared secret out of the
working tree. The key store lands in `data/developer-key.json` relative to where the process was
started (`DataDir`, gitignored).

For the pairing to complete, your StudyAtrium instance has to be able to reach the portal's
internal port (`8081` by default, see [Ports](#ports)): it is the side calling
`POST /internal/register-key` there, not the other way round. Locally that means pointing its
`StudyLifeDevelopers:BaseUrl` at `http://localhost:8081`, not at the UI port.

### As a container

```bash
docker run --rm -p 8080:8080 -p 8081:8081 \
  -e StudyLife__BaseUrl="https://studylife.example.com" \
  -e StudyLifeDevelopers__SharedSecret="<the same value your StudyAtrium instance has>" \
  -e PortalAuth__PublicUrl="http://localhost:8080" \
  -e PortalAuth__StudyLifePublicUrl="https://studylife.example.com" \
  -v studylife-developers-data:/app/data \
  ghcr.io/lukislp/studylife-developers:latest
```

Then open <http://localhost:8080>. The image is multi-arch (`linux/amd64` + `linux/arm64`),
serves the UI on port 8080 (`ASPNETCORE_HTTP_PORTS`) and `/internal/*` on port 8081, runs as the
non-root `app` user (uid 1654) and writes its key store (and the key ring that encrypts the
sign-in cookie) to `/app/data` — the volume above is what makes a completed pairing survive
a container restart. `docker build -t studylife-developers .` builds the same image from this
repo if you'd rather not pull it.

If your StudyAtrium instance serves HTTPS with a certificate from a private CA (the cluster setup
below does), mount that CA's certificate and point `StudyLife__CaCertPath` at it — otherwise the
portal's outbound call fails the TLS handshake:

```bash
  -v /path/to/ca.crt:/etc/studylife-ca/ca.crt:ro \
  -e StudyLife__CaCertPath="/etc/studylife-ca/ca.crt"
```

## Configuration

| Env var | Default | Description |
| --- | --- | --- |
| `StudyLife__BaseUrl` | *(empty)* | Base URL of the paired StudyAtrium instance's API |
| `StudyLifeDevelopers__SharedSecret` | *(empty)* | Must match the studylife repo's `StudyLifeDevelopers:SharedSecret` exactly — authenticates every `/internal/*` call as genuinely coming from StudyAtrium. While it is empty, every `/internal/*` call is rejected with `401` |
| `PortalAuth__PublicUrl` | *(required)* | The portal's canonical external origin as the browser sees it, e.g. `https://studylife-developers.example.com` (no path). `https` is required except for `localhost`/`127.0.0.1`. The sign-in callback is `{PublicUrl}/auth/callback`. Missing or invalid stops the start |
| `PortalAuth__StudyLifePublicUrl` | *(required)* | Browser-facing URL of the paired StudyAtrium web UI (where its consent page lives), e.g. `https://studylife.example.com` — not the cluster-internal `StudyLife__BaseUrl` |
| `PortalAuth__ClientId` | `studylife-developers-portal` | OAuth client id the portal registers on StudyAtrium for its own sign-in. Change it only if another StudyAtrium account already owns that id |
| `PortalAuth__SessionHours` | `8` | Sliding lifetime of a portal session |
| `PortalAuth__SignInPermitsPerMinute` | `20` | Rate limit for `/auth/login` and `/auth/callback` per client address |
| `AllowedHosts` | `localhost` | Host names the portal answers to, `;`-separated, without ports. Any other `Host` header gets `400`, which also defeats DNS rebinding. List every name browsers and StudyAtrium use (gateway host names, the internal Service DNS name) plus `localhost` for the probes. `*` is refused outside the `Development` environment |
| `InternalApi__Port` | `8081`, or `8443` with TLS | Port of the internal listener that serves `/internal/*` (and `/health`) only. With `ServerTls` configured it is the HTTPS listener and defaults to `ServerTls__Port`. Must differ from the public port(s) |
| `StudyLife__CaCertPath` | *(empty)* | Path to a PEM certificate to trust for the StudyAtrium connection, for an instance behind a private CA. Unset — or pointing at a file that doesn't exist — means the system trust store, so a plain-HTTP or publicly-trusted setup needs nothing here |
| `DataDir` | `data` | Directory for the single-file key store (`developer-key.json`) |
| `ASPNETCORE_HTTP_PORTS` | `8080` *(set in the image)* | Public plain-HTTP port(s): the UI and `/health`. `/internal/*` does not exist here |
| `ServerTls__CertPath` / `ServerTls__KeyPath` | *(empty)* | PEM certificate chain and private key. When both are set, the internal listener speaks HTTPS (see [TLS](#tls)). Empty is the explicit opt-out to a plain-HTTP internal listener |
| `ServerTls__Port` | `8443` | Default internal port when TLS is on (`InternalApi__Port` wins if set) |
| `ServerTls__ReloadIntervalSeconds` | `300` | How often renewed certificate files are picked up |

### Sign-in

The portal has no password of its own. It reuses the paired StudyAtrium instance's generic add-on
connect flow, unchanged ([`src/StudyLifeDevelopers/Auth`](src/StudyLifeDevelopers/Auth)):

1. Opening the portal without a session redirects to `/auth/login`. The portal makes sure its own
   sign-in client (`PortalAuth__ClientId`) is registered on StudyAtrium through the developer key
   it already holds — owned by the paired account, with exactly one redirect URI
   (`{PublicUrl}/auth/callback`) and only the `Auth.Whoami` scope, so it grants no study data —
   and sends the browser to StudyAtrium's consent page `/connect/client/{clientId}` with a random
   `state` and a PKCE S256 challenge. That client appears in the add-on list as "used to sign in
   to this portal"; if it is edited or deleted, the next sign-in restores it.
2. You sign in to StudyAtrium there if needed and confirm. StudyAtrium redirects back with a
   single-use assertion.
3. The portal checks the `state` (constant time) against a short-lived, encrypted correlation
   cookie, redeems the assertion server-to-server with the PKCE verifier
   (`POST /api/auth/assertion-exchange`), and compares the resulting StudyAtrium user id with the
   account the developer key belongs to (`GET /api/auth/whoami`). Any other account is refused.
   The API key StudyAtrium returns with the assertion is dropped; each sign-in replaces the
   previous one on StudyAtrium's side, so they never pile up.

The session is an encrypted cookie (`__Host-studylife-developers`: `HttpOnly`, `Secure`,
`SameSite=Lax`, host-only, sliding `PortalAuth__SessionHours`). It is bound to the current pairing:
revoking the connection on StudyAtrium's Setup page, or pairing again (which issues a new key),
ends every session on the next request, and an open Blazor tab within a minute. Every endpoint
without its own rule requires that session (deny-by-default fallback policy), state-changing
requests and the Blazor circuit are refused when their `Origin` names another host, and sign-out
is a POST with an antiforgery token. Without a pairing nobody can sign in; the portal shows how to
enable the connection instead.

### Ports

| Port | Serves | Reached by |
| --- | --- | --- |
| `ASPNETCORE_HTTP_PORTS` (8080) | UI (signed in), `/auth/*`, `/health` | Browsers through the gateway route, kubelet probes |
| `InternalApi__Port` (8081, or 8443 with TLS) | `/internal/*` (shared secret), `/health` | StudyAtrium's backend only |

The separation is decided by the local port a connection arrived on, not by the `Host` header:
`/internal/*` answers `404` on the public port, and the UI answers `404` on the internal one.

### TLS

StudyAtrium's backend sends the shared secret and the user's API key to `/internal/*`, so that hop
is encrypted. With `ServerTls__CertPath` and `ServerTls__KeyPath` set,
[`ServerTls`](src/StudyLifeDevelopers/Hosting/ServerTls.cs) turns the internal listener into an
HTTPS listener (TLS 1.2 and 1.3 only) on `ServerTls__Port` unless `InternalApi__Port` says
otherwise; the public port(s) stay plain HTTP for the kubelet probes and the gateway route. Only one of the two set, or a pair that cannot be
loaded, stops the start, so the portal never runs without the TLS listener it was asked for. The
certificate is re-read when the files change (checked at most every
`ServerTls__ReloadIntervalSeconds`), so a renewed certificate is served without a restart.

On Kubernetes the certificate is a cert-manager `Certificate`
([`k8s/07-tls-certificate.yaml`](k8s/07-tls-certificate.yaml), Flux-managed) from StudyAtrium's
internal CA (`studylife-internal-ca-issuer`) for
`studylife-developers.studylife-developers.svc.cluster.local`,
`studylife-developers.studylife-developers.svc` and `studylife-developers`. StudyAtrium validates it
against that CA, host name included (`AddonTls:CaCertificatePath`, see StudyAtrium's
[SECURITY-ASSURANCE.md](https://github.com/lukislp/studylife/blob/main/docs/SECURITY-ASSURANCE.md)
section 8.3), and with `AddonTls:RequireHttps` refuses `http://` add-on URLs.

StudyAtrium calls `https://studylife-developers.studylife-developers.svc.cluster.local:8443`
(its `StudyLifeDevelopers__BaseUrl`), which is the internal listener on the cluster
(`InternalApi__Port=8443` in `k8s/03-app.yaml`).

Configuration goes through the standard ASP.NET Core provider chain, so every key above works as
an environment variable with `__` separating the sections, as a `dotnet user-secrets` entry, or
straight out of `appsettings.json`.

## Running it on Kubernetes

`k8s/` deploys the portal into its own `studylife-developers` namespace on the cluster it was
written for, reconciled by [Flux](https://fluxcd.io/). The split follows the same convention as
the other apps on that cluster: namespace, Secret, network policies and the HTTPRoute are
bootstrap objects applied once by hand (the Flux reconciler's ClusterRole deliberately cannot
manage those kinds), while `k8s/flux-deploy/` (ConfigMap, PVC, Deployment, Service, CA
ConfigMap, TLS Certificate) is what Flux reconciles continuously.

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/02-secret-sealed.yaml   # or create the Secret out of band, see k8s/02-secret.yaml
kubectl apply -f k8s/04-network-policies.yaml -f k8s/06-httproute.yaml
kubectl apply -f k8s/flux/                   # GitRepository + Kustomization -> k8s/flux-deploy/
```

Without Flux, applying that same curated set once by hand does the job:
`kubectl apply -f k8s/01-config.yaml -f k8s/03-app.yaml -f k8s/05-studylife-ca.yaml -f k8s/07-tls-certificate.yaml`.

For another cluster, adjust `StudyLife__BaseUrl` in `k8s/01-config.yaml` (it points at the paired
instance's in-cluster Service DNS), `AllowedHosts`, `PortalAuth__PublicUrl` and
`PortalAuth__StudyLifePublicUrl` there, and the hostnames in `k8s/06-httproute.yaml`, and replace
`k8s/02-secret-sealed.yaml` — it is encrypted for one specific sealed-secrets controller and is
useless anywhere else. `k8s/05-studylife-ca.yaml` carries the public root certificate of that
StudyAtrium instance's internal CA; substitute your own, or drop `StudyLife__CaCertPath` from the
ConfigMap entirely if your instance has a publicly trusted certificate.

The Deployment runs a single replica with `strategy: Recreate` (the key store is a plain JSON
file on a ReadWriteOnce volume, not built for concurrent writers), as a non-root user with a
read-only root filesystem, all capabilities dropped and `automountServiceAccountToken: false`.
The namespace enforces the `restricted` Pod Security Standard.

## Operating it

- **Health**: `GET /health` returns `{"status":"ok"}` on both ports and backs both the readiness
  and the liveness probe. The probes send `Host: localhost`, since the `Host` header is pinned
  (`AllowedHosts`).
- **State**: exactly one file, `developer-key.json` in `DataDir` — the key the paired StudyAtrium
  instance registered. On the cluster that is a 64Mi Longhorn PVC, opted into the nightly Velero
  PVC backup through the `backup.velero.io/backup-volumes` annotation. Nothing else is
  persisted: the add-on registrations themselves live on the StudyAtrium instance, not here.
- **Pairing lifecycle**: enabling the card on StudyAtrium's Setup page calls
  `POST /internal/register-key` on the internal port, disabling it calls
  `POST /internal/revoke-key`. Both are authenticated by an `X-StudyLife-Shared-Secret` header,
  compared in constant time against `StudyLifeDevelopers__SharedSecret`, so rotating that secret means changing it on both sides —
  until then StudyAtrium's calls get a `401` and the pairing cannot be re-established. Losing the
  key store is recoverable the same way: toggle the card off and on again to re-register.
- **Rollouts**: `Recreate` plus a single ReadWriteOnce volume means every deploy has a real gap
  between the old pod stopping and the new one serving, measured at roughly 15 seconds on that
  cluster. Expected, not a fault.
- **Exposure**: the UI requires StudyAtrium sign-in as the paired account (see
  [Sign-in](#sign-in)), the `Host` header is pinned, and `/internal/*` lives on a port the gateway
  route never reaches. On the cluster it additionally sits behind an internal-only Gateway.
- **Image updates**: on every release the CI pipeline's `deploy-bump` job writes the new version
  into `k8s/03-app.yaml` on `main`, and Flux applies it from there — the image tag in Git is the
  source of truth, and nothing in the cluster pushes back to this repo.
- **Logs**: `kubectl -n studylife-developers logs deploy/studylife-developers`.

## Scopes

The scopes an add-on may request are a fixed, curated list — see
[`Services/ScopeCatalog.cs`](src/StudyLifeDevelopers/Services/ScopeCatalog.cs). It mirrors
`ApiKeyScopes.PubliclyGrantable` in the studylife repo and `schema/known-scopes.json` in
studylife-marketplace; all three are kept in sync by hand.

## Development

The four commands CI runs, in order:

```bash
dotnet restore StudyLifeDevelopers.slnx --locked-mode
dotnet format StudyLifeDevelopers.slnx --verify-no-changes
dotnet build StudyLifeDevelopers.slnx -c Release --no-restore
dotnet test StudyLifeDevelopers.slnx -c Release --no-build
```

Warnings are errors repo-wide (`Directory.Build.props`), and every project carries a
`packages.lock.json`: restore resolves exactly the recorded package graph, and with `CI=true`
set, a csproj that disagrees with its lock file fails the restore instead of silently updating
it. A plain `dotnet restore` refreshes the lock files locally after a package change.

## Releases and images

Versions are cut by the [lktec-bot](https://github.com/lukislp/lktec-bot) App's release module from
[Conventional Commit](https://www.conventionalcommits.org/) messages on `main` — see
[the Releases page](https://github.com/lukislp/studylife-developers/releases) for the generated
history. Each release publishes a multi-arch
image to `ghcr.io/lukislp/studylife-developers`, tagged `latest` and `X.Y.Z`, with an SBOM and a
SLSA provenance attestation attached and a Sigstore keyless signature on the manifest, and is
gated on a Trivy scan finding no fixable `CRITICAL` vulnerability. Verify a tag before pulling:

```bash
cosign verify \
  --certificate-identity-regexp '^https://github\.com/lukislp/studylife-developers/\.github/workflows/ci\.yml@refs/heads/main$' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com \
  ghcr.io/lukislp/studylife-developers:<tag>
```

## Security and privacy

See [SECURITY.md](SECURITY.md) for how to report a vulnerability, and [PRIVACY.md](PRIVACY.md)
for what this portal does and does not store.

## License

AGPL-3.0 — see [LICENSE](LICENSE).
