## [1.5.2](https://github.com/lukislp/studylife-developers/compare/v1.5.1...v1.5.2) (2026-09-20)


### Bug Fixes

* **ci:** add Harden Runner in audit mode to every job ([#57](https://github.com/lukislp/studylife-developers/issues/57)) ([de42601](https://github.com/lukislp/studylife-developers/commit/de426018ffb3b8fbadc8c5b86bf928cfdc667981))

## [1.5.1](https://github.com/lukislp/studylife-developers/compare/v1.5.0...v1.5.1) (2026-09-17)


### Bug Fixes

* **deps:** Bump the dotnet group with 1 update ([1196304](https://github.com/lukislp/studylife-developers/commit/11963046125ab5ab20a7bfa7a6994e3282c832ea))

# [1.5.0](https://github.com/lukislp/studylife-developers/compare/v1.4.13...v1.5.0) (2026-09-16)


### Features

* **scopes:** offer the timer control scope to registering clients ([#50](https://github.com/lukislp/studylife-developers/issues/50)) ([6a1887a](https://github.com/lukislp/studylife-developers/commit/6a1887aaf952f894908d33c94b90d48257b08fad))

## [1.4.13](https://github.com/lukislp/studylife-developers/compare/v1.4.12...v1.4.13) (2026-09-13)


### Bug Fixes

* **k8s:** seal studylife-developers-secrets so it survives a cluster rebuild ([#42](https://github.com/lukislp/studylife-developers/issues/42)) ([375ad6a](https://github.com/lukislp/studylife-developers/commit/375ad6a999cac29724f2acc36619042a40d9e3ed))

## [1.4.12](https://github.com/lukislp/studylife-developers/compare/v1.4.11...v1.4.12) (2026-09-13)


### Bug Fixes

* **k8s:** give probes a real timeout so load spikes stop killing pods ([#41](https://github.com/lukislp/studylife-developers/issues/41)) ([58a2a40](https://github.com/lukislp/studylife-developers/commit/58a2a4090eb78026540d40bfbc4513af0525141a))

## [1.4.11](https://github.com/lukislp/studylife-developers/compare/v1.4.10...v1.4.11) (2026-09-13)


### Bug Fixes

* **k8s:** read-only root filesystem for studylife-developers ([#40](https://github.com/lukislp/studylife-developers/issues/40)) ([77fccb8](https://github.com/lukislp/studylife-developers/commit/77fccb8e3a2dafd70619f8bf3f4db77836324597))

## [1.4.10](https://github.com/lukislp/studylife-developers/compare/v1.4.9...v1.4.10) (2026-09-13)


### Bug Fixes

* **k8s:** close the open egress hole in this namespace ([#39](https://github.com/lukislp/studylife-developers/issues/39)) ([1e1a0ae](https://github.com/lukislp/studylife-developers/commit/1e1a0aec2f0895a49c90480246f5c2b3644a5d35))

## [1.4.9](https://github.com/lukislp/studylife-developers/compare/v1.4.8...v1.4.9) (2026-09-13)


### Bug Fixes

* **k8s:** add explicit egress policy for the developers portal ([#38](https://github.com/lukislp/studylife-developers/issues/38)) ([83b73ba](https://github.com/lukislp/studylife-developers/commit/83b73baa4bb84619ef209a5f18f5d7679147f32e))

## [1.4.8](https://github.com/lukislp/studylife-developers/compare/v1.4.7...v1.4.8) (2026-09-13)


### Bug Fixes

* **k8s:** raise the studylife-developers namespace from PSS baseline to restricted ([#37](https://github.com/lukislp/studylife-developers/issues/37)) ([1458e39](https://github.com/lukislp/studylife-developers/commit/1458e3920fe989cbc2f163be178ab4aa64942c56))

## [1.4.7](https://github.com/lukislp/studylife-developers/compare/v1.4.6...v1.4.7) (2026-09-13)


### Bug Fixes

* **k8s:** opt the KeyStore volume into the nightly Velero backup ([#36](https://github.com/lukislp/studylife-developers/issues/36)) ([71accdc](https://github.com/lukislp/studylife-developers/commit/71accdcbe062a8d4667fc6c016cd5a121473fdf8))

## [1.4.6](https://github.com/lukislp/studylife-developers/compare/v1.4.5...v1.4.6) (2026-09-12)


### Bug Fixes

* **ci:** bump the deployment image tag from the pipeline instead of Flux ([#18](https://github.com/lukislp/studylife-developers/issues/18)) ([77978a5](https://github.com/lukislp/studylife-developers/commit/77978a5029e0458a81e6bb74a61d57e904038614))

## [1.4.5](https://github.com/lukislp/studylife-developers/compare/v1.4.4...v1.4.5) (2026-09-12)


### Bug Fixes

* **deps:** Bump Microsoft.AspNetCore.Mvc.Testing and Microsoft.NET.Test.Sdk ([21ca313](https://github.com/lukislp/studylife-developers/commit/21ca3130c1a16504a0ef047da15f0235795f722c))

## [1.4.4](https://github.com/lukislp/studylife-developers/compare/v1.4.3...v1.4.4) (2026-09-11)


### Bug Fixes

* **ci:** read-only GITHUB_TOKEN in the Dependabot auto-merge workflow ([3b0f9d8](https://github.com/lukislp/studylife-developers/commit/3b0f9d801fd60290b5617733fa061c51a914291e))

## [1.4.3](https://github.com/lukislp/studylife-developers/compare/v1.4.2...v1.4.3) (2026-09-11)


### Bug Fixes

* **ci:** push release commits as a deploy key so the default branch can be ruleset-protected ([9cb19ab](https://github.com/lukislp/studylife-developers/commit/9cb19ab8ea4c3423c5f0a770e97285ee6c65ff91))

## [1.4.2](https://github.com/lukislp/studylife-developers/compare/v1.4.1...v1.4.2) (2026-09-04)


### Bug Fixes

* **ci:** bump actions/setup-dotnet from 5 to 6 ([692f878](https://github.com/lukislp/studylife-developers/commit/692f8788038c10e7504494c0e0ffd4900f5c4323))
* **ci:** bump aquasecurity/trivy-action ([033d6ff](https://github.com/lukislp/studylife-developers/commit/033d6ff250d1c03dbb5735654894b8244474f5f3))
* **ci:** bump docker/setup-buildx-action from 4.2.0 to 4.3.0 ([8c64609](https://github.com/lukislp/studylife-developers/commit/8c64609e2603a89652723bf350a903976e2a4607))
* **deps:** Bump the dotnet group with 2 updates ([45f7add](https://github.com/lukislp/studylife-developers/commit/45f7addfa098d71e5a67e4930479b70ae26b11bc))
* **deps:** Bump xunit.runner.visualstudio from 3.1.5 to 4.0.0 ([5b74fe8](https://github.com/lukislp/studylife-developers/commit/5b74fe88e8d2806ff54c77958b92e777ff010c08))

## [1.4.1](https://github.com/lukislp/studylife-developers/compare/v1.4.0...v1.4.1) (2026-09-03)


### Bug Fixes

* **ci:** add Dependabot for github-actions, nuget, docker ([0e13a24](https://github.com/lukislp/studylife-developers/commit/0e13a247d02948104aedc5daca35186fe5d75a92))

# [1.4.0](https://github.com/lukislp/studylife-developers/compare/v1.3.1...v1.4.0) (2026-08-31)


### Features

* add Metrics.GetSummary to the publicly-grantable scope catalog ([a0d728d](https://github.com/lukislp/studylife-developers/commit/a0d728ddafce1c906ae70acfb426992ca5f5d2c1)), closes [studylife#114](https://github.com/studylife/issues/114)

## [1.3.1](https://github.com/lukislp/studylife-developers/compare/v1.3.0...v1.3.1) (2026-08-29)


### Bug Fixes

* tighten readiness probe timing to shrink the deploy downtime window ([0f6f955](https://github.com/lukislp/studylife-developers/commit/0f6f95564af932f18eb99824ac644772c416446c))

# [1.3.0](https://github.com/lukislp/studylife-developers/compare/v1.2.5...v1.3.0) (2026-08-29)


### Features

* replace the add-on modal with a full page ([a598afd](https://github.com/lukislp/studylife-developers/commit/a598afd3a80108bf00d546340316b682b7f885f5))

## [1.2.5](https://github.com/lukislp/studylife-developers/compare/v1.2.4...v1.2.5) (2026-08-29)


### Bug Fixes

* modal body wasn't actually scrollable (flex child needed min-height:0) ([a4ff9a6](https://github.com/lukislp/studylife-developers/commit/a4ff9a6406ea735f0774acacd0da27a856c34c40))

## [1.2.4](https://github.com/lukislp/studylife-developers/compare/v1.2.3...v1.2.4) (2026-08-29)


### Bug Fixes

* add favicon, cap the add-on modal height, fix scope checkbox alignment ([e68ac58](https://github.com/lukislp/studylife-developers/commit/e68ac582ff25234d3902ebef70f0f600f25a391f))

## [1.2.3](https://github.com/lukislp/studylife-developers/compare/v1.2.2...v1.2.3) (2026-08-29)


### Bug Fixes

* build with a single-step restore+publish instead of the split-layer pattern ([11d46ba](https://github.com/lukislp/studylife-developers/commit/11d46ba8b7a0a321abc12578c38ec5ae5931cab6))

## [1.2.2](https://github.com/lukislp/studylife-developers/compare/v1.2.1...v1.2.2) (2026-08-29)


### Bug Fixes

* serve blazor.web.js by mapping static assets instead of UseStaticFiles ([2eed448](https://github.com/lukislp/studylife-developers/commit/2eed448f23a20964cb5d7004ab58069c3ccc8ec7))

## [1.2.1](https://github.com/lukislp/studylife-developers/compare/v1.2.0...v1.2.1) (2026-08-29)


### Bug Fixes

* allow the shared NGF Gateway to reach the app pod (502 fix) ([8da12d6](https://github.com/lukislp/studylife-developers/commit/8da12d6ff4887b1c9cd92e22496977306dd6ee1b))

# [1.2.0](https://github.com/lukislp/studylife-developers/compare/v1.1.1...v1.2.0) (2026-08-29)


### Features

* add HTTPRoute for browser access to the portal UI ([1e31ba6](https://github.com/lukislp/studylife-developers/commit/1e31ba663badfb43ef5ca8cdcf2e7dd210ef60c6)), closes [#22](https://github.com/lukislp/studylife-developers/issues/22)

## [1.1.1](https://github.com/lukislp/studylife-developers/compare/v1.1.0...v1.1.1) (2026-08-29)


### Bug Fixes

* publish multi-arch amd64/arm64 images so the app can run on arm64 cluster nodes ([d4bb07f](https://github.com/lukislp/studylife-developers/commit/d4bb07f600ff9311ef8fe4c5659f96baa559cd88))

# [1.1.0](https://github.com/lukislp/studylife-developers/compare/v1.0.0...v1.1.0) (2026-08-29)


### Features

* add production k8s manifests and Flux GitOps wiring ([bcaec8d](https://github.com/lukislp/studylife-developers/commit/bcaec8da39259e3d5e6b8e5dc4f27976aef90a0c))

# 1.0.0 (2026-08-29)


### Features

* scaffold the StudyLife Developers portal ([72f2749](https://github.com/lukislp/studylife-developers/commit/72f2749868158f83ea4337075337729b6e0d7435))
