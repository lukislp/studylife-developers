# Privacy Policy - StudyAtrium Developers

StudyAtrium Developers is a portal extending a self-hosted
[StudyAtrium](https://github.com/lukislp/studylife) instance. There is no vendor server involved -
this portal is self-hosted alongside your own StudyAtrium instance, and your data goes exactly two
places: your own paired StudyAtrium instance's API, and this portal's own local single-file key
store.

## What this portal reads

- **Nothing directly from you** beyond what you type into it (add-on names, descriptions,
  redirect URIs, scope selections) - it forwards that to your paired StudyAtrium instance's
  `/api/developer/clients` endpoints, on your behalf, using the one key StudyAtrium issued it.
- **A key from StudyAtrium itself**, only over `/internal/*` endpoints authenticated by one shared
  secret you configure on both sides.
- **Your StudyAtrium user id** when you sign in, to check that you are the paired account. The
  sign-in grants the portal no access to your study data.

## What this portal stores

Locally, in a single file on disk, never shared with StudyAtrium or anyone else:

- The one API key StudyAtrium issued this portal (see `/internal/register-key`).
- The ASP.NET Core Data Protection key ring that encrypts the sign-in cookie.

In your browser it sets one session cookie (your StudyAtrium user id and a fingerprint of the
pairing, encrypted) and, during sign-in, a short-lived correlation cookie. No tracking cookies.

## What this portal sends

- Whatever you enter when registering or editing an add-on (name, description, redirect URIs,
  requested scopes) to your paired StudyAtrium instance's `/api/developer/clients` endpoints -
  nowhere else.

## Fonts and other resources

The portal's fonts (DM Sans and DM Mono, SIL Open Font License 1.1) are served by the portal
itself from `wwwroot/fonts`. Your browser does not contact Google or any other font host, CDN or
third party when it loads a page. Earlier versions loaded the fonts from Google Fonts, which sent
your IP address to Google; that is no longer the case.

## What this portal never does

- Never collects analytics, telemetry, or crash reports.
- Never contacts any server other than the StudyAtrium instance it's paired to. Your browser
  likewise loads every page resource (styles, fonts, scripts, icons) from the portal only.
- Never hosts, runs, or has access to any add-on's actual code - only its registration metadata.

## Controller and publisher

Controller: the operator of your instance. Publisher: see the imprint at
<https://lktec.org/impressum>.

## Source

This portal is open source (AGPL-3.0): <https://github.com/lukislp/studylife-developers>.
