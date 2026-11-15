# Release process

Version 1.0.0 is the current candidate. [Its release record](releases/1.0.0.md)
tracks exact-package upgrades, hosted gates and publication. No public v1 release
is claimed until those steps succeed.

The latest verified public preview is 0.1.0-alpha.7, published November 13, 2026
from `c5d639bcff707ff1a75b23f2a60135d6be6cb7b2`. Its [release record](releases/0.1.0-alpha.7.md)
links exact-commit correctness, package and hosted performance gates, publication
and fresh-installation hashes. Directory.Build.props now carries the distinct 1.0.0 candidate
version; alpha.7 remains the public baseline until v1 is verified.
Do not overwrite or republish alpha.7 to include the current working tree.

The v1 candidate adopts the supported API contract under MIT. The verified
independent consumer matrix is Windows 11 x64 / .NET 10.0.13 with SDK 10.0.402;
this is evidence, not a promise of an untested platform/runtime matrix. Install a
compatible stable .NET 10 SDK; global.json does not accept an 11 preview as a substitute.

## Next release candidate

1. Reconcile the Unreleased changes and finalize the candidate's supported API and
   Windows/runtime scope. Completed scoped items remain complete; optional APIs
   and post-v1 FW-025/026 work do not silently become new release blockers.
2. Pack a unique candidate version and verify the independent sample against those
   exact packages, including the upgrade from published alpha.7. FW-019's completed
   alpha.6-to-alpha.7 comparison does not measure unpublished changes.
3. Run full correctness/package checks and mandatory local paired performance
   diagnostics against the unchanged accepted baseline. Commit the exact candidate,
   then require the full hosted performance comparison and correctness checks.
4. Publish only the validated artifacts and verify a new public restore and assembly
   hashes. A future v1 RC still requires its own exact-package acceptance.

Historical physical IME/contrast/scaling evidence keeps its documented scope and
missing environment details. The additional post-cleanup Narrator revalidation is
post-v1 FW-026 by user decision. Do not claim it ran, or present static captures as
new physical input/accessibility acceptance. See the [current backlog](backlog.md).

## Required owner decisions

- MIT licensing is selected and the approved license text is in LICENSE.
- Public repository: https://github.com/signalnotnoise/ui-framework. Verify the staged source files before uploading.
- NuGet IDs: `SignalNotNoise.UI` and `SignalNotNoise.UI.Wpf`; authors: UI Framework contributors. Only the two libraries are packable. Both include a package README, MIT license, repository metadata, and separate portable symbol packages. Both IDs are published; verify account permissions and the Trusted Publishing policy before a new release.

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

The CLI also pushes the adjacent symbol package. Package versions are immutable: increment the prerelease suffix for corrections, and never use a retry to replace published contents. After indexing, repeat installation in a fresh WPF project using NuGet.org and update the README availability text. Local preparation alone does not publish packages. Version `0.1.0-alpha.1` was published on September 18, 2026 from commit `78c88fb` by [Publish NuGet run 1](https://github.com/signalnotnoise/ui-framework/actions/runs/35395414259). Version `0.1.0-alpha.3` was published on September 26, 2026 from commit `520173d` by [Publish NuGet run 6](https://github.com/signalnotnoise/ui-framework/actions/runs/36231322862). Both package IDs were indexed, and a new WPF project restored the core and WPF packages from NuGet.org using an empty package cache and completed a zero-warning Release build. The trusted policy, `NUGET_USER=signalNotNoise`, and `nuget` environment restricted to `main` are configured.

## Validation

Run tools/Test-Documentation.ps1 to verify the README, latest versioned changelog entry and knowledge graph against Directory.Build.props, and to reject mutable workflow action references. Test-Release includes this check. Action SHAs were resolved from the existing major-version tags; Dependabot checks for updates weekly. Keep the human-readable version comment beside each SHA.

The test dependencies remain explicitly pinned together in the single test project. Upgrade the MSTest adapter and framework as a pair, then run Test-Release on the repository's .NET 10 SDK and Windows WPF runtime. Central package management is deferred until multiple projects need shared package versions; introducing it alone would not improve the current runtime contract.

Apply the agreed v1 acceptance scope in the backlog and retain the existing physical acceptance records with their limits. Automated peer/focus tests do not establish unperformed physical outcomes; exact-candidate package and hosted checks remain mandatory. Prerelease APIs may change without compatibility guarantees; behavioral changes belong in the Unreleased changelog and require a new package version before publishing.

For `0.1.0-alpha.3`, visible typing, selection, replace, undo and redo; installed-IME composition start, update, commit and cancel; screen-reader names, focus, state and text-change announcements; and repeated open/close without cleanup errors or lingering processes were accepted manually before publication.

Local verification on September 18, 2026: Release build completed with zero warnings/errors; 41 MSTest tests, 15 visual-stress checks and 21 full-stress assertions passed. The isolated package consumer also passed. Full stress results are recorded in artifacts/release-validation/stress.json. Hosted validation and publishing both passed. After NuGet indexing completed, a fresh WPF consumer restored both published packages from NuGet.org using an empty cache and passed rendering, click, retained-control and observable-update checks.

Run `./tools/Test-Release.ps1` from Windows PowerShell or PowerShell 7. The same checks are configured in .github/workflows/validate.yml. Local evidence is stored under artifacts/release-validation and is excluded from source control.

`validate.yml` builds and validates only, with read-only repository permissions. `publish-nuget.yml` is manually triggered and repeats validation before its environment-scoped publishing job.

## Source release

Run `./tools/New-SourceArchive.ps1` to generate artifacts/ui-framework-0.1.0-alpha.1-source.zip. The script includes an explicit set of source/documentation directories and file types, validates required files, and excludes build outputs and user settings. It does not upload or publish the archive.

Exclude bin, obj, .vs, TestResults, artifacts, local environment files and user-specific IDE settings. Include source, project files, docs, tools, README, CHANGELOG, CONTRIBUTING and LICENSE. Review the release contents for accidental private data before upload. The source repository is public; keep future release source commits pushed before running the publishing workflow.

An optional GitHub prerelease can attach the source archive after validation. Describe it as experimental, Windows-only for rendering, requiring .NET 10. Do not present historical benchmarks as performance guarantees or overstate the scope of recorded focus/IME/accessibility verification.

No repository, tag, release or package is created by the validation script.
