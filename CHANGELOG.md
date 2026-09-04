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
