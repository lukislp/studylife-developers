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
