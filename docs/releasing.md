# Release process

Version 1.3.0 is being prepared for FW-040/FW-041; [current release status](releases/1.3.0.md). Publication requires exact-commit hosted gates.

Version 1.3.0 is being prepared for FW-040/FW-041; [current release status](releases/1.3.0.md). Publication requires exact-commit hosted gates.

Version 1.2.0 was published November 20, 2026; see [its release record](releases/1.2.0.md).

The 1.1.0 animation release was published November 20, 2026. Its exact source,
hosted gates and public verification are tracked in [the release record](releases/1.1.0.md).

Version 1.0.0 was published November 15, 2026 from
`cbd85b8beb2e3aa57b709dd96b4943365db02f57`. Both hosted workflows passed,
including 32 performance checks each. [The detailed release record](releases/1.0.0.md)
links package upgrades, exact-commit gates, publication and verification evidence.
The prior [alpha.7 release](releases/0.1.0-alpha.7.md) remains the independent
upgrade baseline. Never overwrite either published version to include later changes.

V1 adopts the supported API contract under MIT. The verified
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

### Decide performance coverage before pushing

For a push containing only reviewed non-runtime changes, put the literal marker
`[skip performance]` in the **tip commit message**. GitHub evaluates this at the
job level before allocating the performance runner, avoiding benchmark setup
and comparison minutes. It does not skip graph, correctness or package checks.
Unmarked pushes, all PRs, manual Validate runs and Publish NuGet still run the
full comparison. Never use `[skip ci]` for this purpose.

Before creating the commit, stage the intended changes and check the **entire
proposed pushed range**, not just the final commit:

```powershell
git fetch origin main
python scripts/performance-required.py --base origin/main --staged
# Only if the check exits 0 and the changed paths have been reviewed:
git commit -m "Update agent guidance [skip performance]"
# Check the final committed range again before pushing:
python scripts/performance-required.py --base origin/main
git push origin main
```

Use the actual destination remote branch in place of `origin/main`. Exit 0 means
eligible; exit 1 means keep the full comparison. A freshly fetched remote ref
is required; the helper does not fetch or change commits. Rerun after any edits,
rebases, merges or remote changes. If eligibility changes, remove the marker
before pushing. Do not assume an older marker is valid for a new pushed range.
For multiple commits, only the pushed tip's message controls the job, but the
local check must include every change since the destination branch.

`scripts/performance-required.py` has a narrow allowlist: selected prose docs,
generated architecture graphs, skill content and its reference generator/tests,
documentation images and correctness-test C# sources. Runtime/templates, samples,
project/SDK settings, workflows, benchmark code, baseline/budgets, performance
policy documentation and unknown paths require comparison. Renames preserve
both source and destination paths. Missing history and empty ranges also require
comparison. Extend the allowlist only after reviewing dependencies.

This is an explicit maintainer decision: GitHub trusts the marker and does not
repeat the path analysis on a runner. A skipped comparison is **not performance
acceptance evidence**. Publication still requires a successful full hosted
comparison against the unchanged accepted baseline for the exact candidate,
even for documentation-only changes. Local runtime-change measurement duties
remain unchanged. This scheduling change does not alter benchmark workloads,
budgets, accepted revisions or measurement commands.

### Fast documentation and skill-content pushes

For an eligible documentation/skill-content-only range, use `[docs only]`
instead. This skips both the performance and Windows release jobs **before
runner allocation**. The graph/tooling job still runs reference freshness,
generator regression checks, graph validation and other Python tooling checks.
The marker does not apply to PRs, manual Validate runs or Publish NuGet.

```powershell
git fetch origin main
python scripts/performance-required.py --base origin/main --staged --docs-only
# Only after a reviewed exit-0 result:
git commit -m "Clarify component guidance [docs only]"
python scripts/performance-required.py --base origin/main --docs-only
git push origin main
```

The same fresh-destination and whole-pushed-range rules apply. This narrower
mode excludes all code/tool/test changes, including the compiled skill examples,
even when those changes qualify for `[skip performance]`. C# or build changes
must keep automatic regression validation. Unmarked pushes keep the existing
full Windows validation, including native stress, startup, API and package checks.
GitHub trusts either marker; do not use one to bypass a failing required check.

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
