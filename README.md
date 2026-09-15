# StudyLife Developers

[![CI](https://github.com/lukislp/studylife-developers/actions/workflows/ci.yml/badge.svg)](https://github.com/lukislp/studylife-developers/actions/workflows/ci.yml) [![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/lukislp/studylife-developers/badge)](https://scorecard.dev/viewer/?uri=github.com/lukislp/studylife-developers) [![CodeQL](https://github.com/lukislp/studylife-developers/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lukislp/studylife-developers/security/code-scanning)
[![Release](https://img.shields.io/github/v/release/lukislp/studylife-developers)](https://github.com/lukislp/studylife-developers/releases)
[![License: AGPL-3.0](https://img.shields.io/github/license/lukislp/studylife-developers)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

A developer portal for registering [StudyLife](https://github.com/lukislp/studylife) add-ons —
paired to exactly one StudyLife instance, like every other satellite in this ecosystem (Tray,
Webhooks, HACS, MCP). Register a new add-on here, pick which scopes it needs, get a `ClientId`
and a redirect URI to build into your app, then submit it to
[studylife-marketplace](https://github.com/lukislp/studylife-marketplace) once it's ready for
other people to install.

It is a small ASP.NET Core / Blazor Server app — one page listing your registered add-ons, one
registration form, a single-file key store, and two `/internal/*` endpoints its paired StudyLife
instance calls. No database, and no login screen of its own; see
[Operating it](#operating-it) for what that means for where you expose it.

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

## Quick start

You need a StudyLife instance to pair with: the portal talks to that instance's
`/api/developer/*` endpoints, and it only becomes usable once the instance has pushed its key
here (step 1 above). Until then the UI shows a "not connected" state instead of attempting any
request.

### Locally (.NET 10 SDK)

```bash
dotnet restore StudyLifeDevelopers.slnx
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyLife:BaseUrl" "https://studylife.example.com"
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyLifeDevelopers:SharedSecret" "<the same value your StudyLife instance has>"
dotnet run --project src/StudyLifeDevelopers --urls http://localhost:5080
```

Then open <http://localhost:5080>. Both settings work equally as environment variables
(`StudyLife__BaseUrl`, `StudyLifeDevelopers__SharedSecret`) or as edits to
`src/StudyLifeDevelopers/appsettings.json` — user secrets just keep the shared secret out of the
working tree. The key store lands in `data/developer-key.json` relative to where the process was
started (`DataDir`, gitignored).

For the pairing to complete, your StudyLife instance has to be able to reach this URL: it is the
side calling `POST /internal/register-key` here, not the other way round.

### As a container

```bash
docker run --rm -p 8080:8080 \
  -e StudyLife__BaseUrl="https://studylife.example.com" \
  -e StudyLifeDevelopers__SharedSecret="<the same value your StudyLife instance has>" \
  -v studylife-developers-data:/app/data \
  ghcr.io/lukislp/studylife-developers:latest
```

Then open <http://localhost:8080>. The image is multi-arch (`linux/amd64` + `linux/arm64`),
listens on port 8080 (`ASPNETCORE_HTTP_PORTS`), runs as the non-root `app` user (uid 1654) and
writes its key store to `/app/data` — the volume above is what makes a completed pairing survive
a container restart. `docker build -t studylife-developers .` builds the same image from this
repo if you'd rather not pull it.

If your StudyLife instance serves HTTPS with a certificate from a private CA (the cluster setup
below does), mount that CA's certificate and point `StudyLife__CaCertPath` at it — otherwise the
portal's outbound call fails the TLS handshake:

```bash
  -v /path/to/ca.crt:/etc/studylife-ca/ca.crt:ro \
  -e StudyLife__CaCertPath="/etc/studylife-ca/ca.crt"
```

## Configuration

| Env var | Default | Description |
| --- | --- | --- |
| `StudyLife__BaseUrl` | *(empty)* | Base URL of the paired StudyLife instance's API |
| `StudyLifeDevelopers__SharedSecret` | *(empty)* | Must match the studylife repo's `StudyLifeDevelopers:SharedSecret` exactly — authenticates every `/internal/*` call as genuinely coming from StudyLife. While it is empty, every `/internal/*` call is rejected with `401` |
| `StudyLife__CaCertPath` | *(empty)* | Path to a PEM certificate to trust for the StudyLife connection, for an instance behind a private CA. Unset — or pointing at a file that doesn't exist — means the system trust store, so a plain-HTTP or publicly-trusted setup needs nothing here |
| `DataDir` | `data` | Directory for the single-file key store (`developer-key.json`) |
| `ASPNETCORE_HTTP_PORTS` | `8080` *(set in the image)* | Port Kestrel listens on |

Configuration goes through the standard ASP.NET Core provider chain, so every key above works as
an environment variable with `__` separating the sections, as a `dotnet user-secrets` entry, or
straight out of `appsettings.json`.

## Running it on Kubernetes

`k8s/` deploys the portal into its own `studylife-developers` namespace on the cluster it was
written for, reconciled by [Flux](https://fluxcd.io/). The split follows the same convention as
the other apps on that cluster: namespace, Secret, network policies and the HTTPRoute are
bootstrap objects applied once by hand (the Flux reconciler's ClusterRole deliberately cannot
manage those kinds), while `k8s/flux-deploy/` (ConfigMap, PVC, Deployment, Service, CA
ConfigMap) is what Flux reconciles continuously.

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/02-secret-sealed.yaml   # or create the Secret out of band, see k8s/02-secret.yaml
kubectl apply -f k8s/04-network-policies.yaml -f k8s/06-httproute.yaml
kubectl apply -f k8s/flux/                   # GitRepository + Kustomization -> k8s/flux-deploy/
```

Without Flux, applying that same curated set once by hand does the job:
`kubectl apply -f k8s/01-config.yaml -f k8s/03-app.yaml -f k8s/05-studylife-ca.yaml`.

For another cluster, adjust `StudyLife__BaseUrl` in `k8s/01-config.yaml` (it points at the paired
instance's in-cluster Service DNS) and the hostnames in `k8s/06-httproute.yaml`, and replace
`k8s/02-secret-sealed.yaml` — it is encrypted for one specific sealed-secrets controller and is
useless anywhere else. `k8s/05-studylife-ca.yaml` carries the public root certificate of that
StudyLife instance's internal CA; substitute your own, or drop `StudyLife__CaCertPath` from the
ConfigMap entirely if your instance has a publicly trusted certificate.

The Deployment runs a single replica with `strategy: Recreate` (the key store is a plain JSON
file on a ReadWriteOnce volume, not built for concurrent writers), as a non-root user with a
read-only root filesystem, all capabilities dropped and `automountServiceAccountToken: false`.
The namespace enforces the `restricted` Pod Security Standard.

## Operating it

- **Health**: `GET /health` returns `{"status":"ok"}` and backs both the readiness and the
  liveness probe.
- **State**: exactly one file, `developer-key.json` in `DataDir` — the key the paired StudyLife
  instance registered. On the cluster that is a 64Mi Longhorn PVC, opted into the nightly Velero
  PVC backup through the `backup.velero.io/backup-volumes` annotation. Nothing else is
  persisted: the add-on registrations themselves live on the StudyLife instance, not here.
- **Pairing lifecycle**: enabling the card on StudyLife's Setup page calls
  `POST /internal/register-key`, disabling it calls `POST /internal/revoke-key`. Both are
  authenticated by an `X-StudyLife-Shared-Secret` header against
  `StudyLifeDevelopers__SharedSecret`, so rotating that secret means changing it on both sides —
  until then StudyLife's calls get a `401` and the pairing cannot be re-established. Losing the
  key store is recoverable the same way: toggle the card off and on again to re-register.
- **Rollouts**: `Recreate` plus a single ReadWriteOnce volume means every deploy has a real gap
  between the old pod stopping and the new one serving, measured at roughly 15 seconds on that
  cluster. Expected, not a fault.
- **Exposure**: the portal has no login screen of its own — it is single-tenant, paired to one
  instance. On the cluster it sits behind an internal-only Gateway, at the same trust level as
  the other internal services there. Put it behind something that authenticates before exposing
  it anywhere less trusted.
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

Versions are cut by [semantic-release](https://semantic-release.gitbook.io/) from
[Conventional Commit](https://www.conventionalcommits.org/) messages on `main` — see
[CHANGELOG.md](CHANGELOG.md) for the generated history. Each release publishes a multi-arch
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
