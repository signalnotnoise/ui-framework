# Preparing an experimental release

The project name is ui-framework and the license is MIT, credited to UI Framework contributors. The candidate version is 0.1.0-alpha.2, defined in Directory.Build.props. Source archives and NuGet packages are intended for experimental evaluation.

## Required owner decisions

- MIT licensing is selected and the approved license text is in LICENSE.
- Public repository: https://github.com/signalnotnoise/ui-framework. Verify the staged source files before uploading.
- NuGet IDs: `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf`; authors: UI Framework contributors. Only the two libraries are packable. Both include a package README, MIT license, repository metadata, and separate portable symbol packages. NuGet.org returned no published versions for either ID on September 18, 2026; this does not reserve the names or guarantee permission to publish under a reserved prefix.

## NuGet packages

Run `./tools/Test-Packages.ps1` to build the two packages under `artifacts/packages`, then restore and run a fresh WPF consumer using only that local feed and an isolated package cache. It checks the transitive core dependency, initial rendering, button events, observable updates, and retained control identity. After a Release build, use `-NoBuild` to reuse its binaries. Each validation gets a new consumer/cache directory so a stale package cannot hide a packaging defect.

The Validate workflow runs the same check and uploads the validated `.nupkg` and `.snupkg` files. Symbols allow debugging. The separate Publish NuGet workflow builds from committed `main`, runs full release validation and package installation checks on Windows, and publishes those exact artifacts from a separate job.

## GitHub Actions trusted publishing

Use [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). No long-lived API key or GitHub publishing secret is needed. Configure this once:

1. In GitHub repository Settings > Environments, create `nuget`, restrict deployment branches to `main`, and optionally require a release reviewer.
2. In Settings > Secrets and variables > Actions > Variables, create `NUGET_USER` containing `signalNotNoise`, your NuGet.org profile username (not email). This username is not a secret.
3. In NuGet.org Account > Trusted Publishing, add a GitHub policy with Repository Owner `signalnotnoise`, Repository `ui-framework`, Workflow File `publish-nuget.yml` (filename only), Environment `nuget`. Select the intended NuGet package owner. Permit new packages and new versions, scoped to exact package IDs `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf` on separate lines.
4. Push the committed packaging and workflow changes to GitHub. In Actions > Publish NuGet > Run workflow, choose branch `main` and enter the exact version from `Directory.Build.props` (initially `0.1.0-alpha.1`).

The workflow rejects other branches or mismatched versions. The validation job has no publishing permission; only the publishing job requests an OIDC identity token (`id-token: write`). `NuGet/login@v1` exchanges it for a temporary credential immediately before the push. The environment name must match the NuGet policy. Core publishes before WPF. Runs are serialized; duplicate versions are skipped to permit retries after partial publication.

The CLI also pushes the adjacent symbol package. Package versions are immutable: increment the prerelease suffix for corrections, and never use a retry to replace published contents. After indexing, repeat installation in a fresh WPF project using NuGet.org and update the README availability text. Local preparation alone does not publish packages. Version `0.1.0-alpha.1` was published on September 18, 2026 from commit `78c88fb` by [Publish NuGet run 1](https://github.com/signalnotnoise/ui-framework/actions/runs/35395414259). The trusted policy, `NUGET_USER=signalNotNoise`, and `nuget` environment restricted to `main` are configured.

## Validation

Local verification on September 18, 2026: Release build completed with zero warnings/errors; 41 MSTest tests, 15 visual-stress checks and 21 full-stress assertions passed. The isolated package consumer also passed. Full stress results are recorded in artifacts/release-validation/stress.json. Hosted validation and publishing both passed. After NuGet indexing completed, a fresh WPF consumer restored both published packages from NuGet.org using an empty cache and passed rendering, click, retained-control and observable-update checks.

Run `./tools/Test-Release.ps1` from Windows PowerShell or PowerShell 7. The same checks are configured in .github/workflows/validate.yml. Local evidence is stored under artifacts/release-validation and is excluded from source control.

`validate.yml` builds and validates only, with read-only repository permissions. `publish-nuget.yml` is manually triggered and repeats validation before its environment-scoped publishing job.

## Source release

Run `./tools/New-SourceArchive.ps1` to generate artifacts/ui-framework-0.1.0-alpha.1-source.zip. The script includes an explicit set of source/documentation directories and file types, validates required files, and excludes build outputs and user settings. It does not upload or publish the archive.

Exclude bin, obj, .vs, TestResults, artifacts, local environment files and user-specific IDE settings. Include source, project files, docs, tools, README, CHANGELOG, CONTRIBUTING and LICENSE. Review the release contents for accidental private data before upload. The source repository is public; keep future release source commits pushed before running the publishing workflow.

An optional GitHub prerelease can attach the source archive after validation. Describe it as experimental, Windows-only for rendering, requiring .NET 10. Do not present historical benchmarks as performance guarantees or claim focus/IME/accessibility verification.

No repository, tag, release or package is created by the validation script.
