# Privacy Policy - StudyLife Developers

StudyLife Developers is a portal extending a self-hosted
[StudyLife](https://github.com/lukislp/studylife) instance. There is no vendor server involved -
this portal is self-hosted alongside your own StudyLife instance, and your data goes exactly two
places: your own paired StudyLife instance's API, and this portal's own local single-file key
store.

## What this portal reads

- **Nothing directly from you** beyond what you type into it (add-on names, descriptions,
  redirect URIs, scope selections) - it forwards that to your paired StudyLife instance's
  `/api/developer/clients` endpoints, on your behalf, using the one key StudyLife issued it.
- **A key from StudyLife itself**, only over `/internal/*` endpoints authenticated by one shared
  secret you configure on both sides.

## What this portal stores

Locally, in a single file on disk, never shared with StudyLife or anyone else:

- The one API key StudyLife issued this portal (see `/internal/register-key`).

## What this portal sends

- Whatever you enter when registering or editing an add-on (name, description, redirect URIs,
  requested scopes) to your paired StudyLife instance's `/api/developer/clients` endpoints -
  nowhere else.

## What this portal never does

- Never collects analytics, telemetry, or crash reports.
- Never contacts any server other than the StudyLife instance it's paired to.
- Never hosts, runs, or has access to any add-on's actual code - only its registration metadata.

## Source

This portal is open source (AGPL-3.0): <https://github.com/lukislp/studylife-developers>.
