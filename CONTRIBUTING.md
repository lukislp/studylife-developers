# Contributing to StudyLife Developers

Thanks for taking the time. The StudyLife developer portal is a single-maintainer project, so the
process is deliberately small - but it is the same for every change, including the maintainer's
own.

## How changes get in

1. Open an issue first for anything bigger than a typo or an obvious bug fix, so the direction can
   be agreed before you spend time on it. Use the templates under `.github/ISSUE_TEMPLATE/`.
2. Fork the repository (or branch, if you have write access) and make your change on a branch.
3. Open a pull request against `main`. The pull-request template asks for what changed and why.
4. `main` is protected: a PR merges only after the test stage of
   [`.github/workflows/ci.yml`](.github/workflows/ci.yml) is green and the branch is up to date
   with `main` (enable auto-merge and it lands on its own once that is the case). Nobody pushes to
   `main` directly, not even the maintainer.

## What a pull request needs

- **Conventional Commits.** The version and the changelog are generated from the commit messages
  (`feat:` = minor release, `fix:` = patch release, `build:`/`ci:`/`docs:`/`test:` = no release).
  Squash-merge keeps the PR title as the commit message, so give the PR a Conventional Commit
  title.
- **Green required checks.** `build-and-test` and `review / dependency-review` are required; a red
  one blocks the merge.
- **Tests for new functionality.** New features and bug fixes come with tests in
  `tests/StudyLifeDevelopers.Tests` (xUnit, with `Microsoft.AspNetCore.Mvc.Testing` for endpoint
  tests and FsCheck for the property tests). A PR that adds behaviour without a test is asked to
  add one.
- **Formatting and warnings.** `dotnet format StudyLifeDevelopers.slnx --verify-no-changes` runs
  inside `build-and-test`; run `dotnet format StudyLifeDevelopers.slnx` before pushing. Warnings
  are errors repo-wide (`TreatWarningsAsErrors` in `Directory.Build.props`) - do not silence one
  without saying why in the PR.
- **Lock files.** Every project carries a `packages.lock.json` and CI restores with
  `--locked-mode`, so a csproj that disagrees with its lock file fails the restore instead of
  silently updating it. A plain `dotnet restore StudyLifeDevelopers.slnx` refreshes them locally
  after a package change - commit the result.
- **The shared secret stays out of the repo.** The portal pairs with a StudyLife instance over
  `StudyLifeDevelopers:SharedSecret`. Use user-secrets locally; never commit a real value or a
  real instance URL.

## Running things locally

Requires the .NET 10 SDK.

```bash
dotnet restore StudyLifeDevelopers.slnx
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyLife:BaseUrl" "https://studylife.example.com"
dotnet user-secrets --project src/StudyLifeDevelopers set "StudyLifeDevelopers:SharedSecret" "<the same value your StudyLife instance has>"
dotnet run --project src/StudyLifeDevelopers --urls http://localhost:5080
```

The four commands CI runs, in order:

```bash
dotnet restore StudyLifeDevelopers.slnx --locked-mode
dotnet format StudyLifeDevelopers.slnx --verify-no-changes
dotnet build StudyLifeDevelopers.slnx -c Release --no-restore
dotnet test StudyLifeDevelopers.slnx -c Release --no-build
```

With `CI=true` set, a csproj that disagrees with its lock file fails the restore instead of
silently updating it.

## Security issues

Please do not open a public issue for a vulnerability - use the private reporting path described
in [SECURITY.md](SECURITY.md). The [Code of Conduct](CODE_OF_CONDUCT.md) applies to every
interaction in this repository.
