# FW-019 independent adoption and upgrades — November 15, 2026

Subsequent same-day [FW-012 reconciliation](fw012-release-reconciliation-2026-11-15.md)
recovered the completed alpha.7 publication record and closed FW-024. References
to missing publication history below describe the state during this adoption run.

FW-019 is complete for the published alpha.6 → alpha.7 Windows/WPF package path.
The [standalone PackageWorkspace](../samples/PackageWorkspace/README.md) restores
only SignalNotNoise.UI.Wpf and its transitive core package from NuGet.org. It has
its own build-properties boundary, NuGet configuration and exact-version lock
file. Acceptance copies it outside the checkout into new temporary directories;
both versions use separate empty package/HTTP caches. Resolved assets contain no
source-project dependencies and both framework packages resolve to the requested
version. NuGet currently lists alpha.7 despite the older local release record.

## Adoption and compatibility

The sample provides editable forms, a 100-item selectable list, a split workspace,
navigation and a retained native multiline notes editor. Native event ownership,
application model lifetime, startup, ViewHost disposal, cleanup policy closure and
dispatcher shutdown are explicit in the sample and its README. Data is session-only.
The native island deliberately retains WPF's default styling.

The final campaign passes **14/14 independent processes**:

- Three alternating alpha.6/alpha.7 pairs with a full list.
- Three alternating alpha.6/alpha.7 pairs with virtualization.
- One binary-upgrade process for each list mode: keep the alpha.6 consumer DLL,
  deps/runtime files and other outputs unchanged; replace only the two framework
  DLLs with published alpha.7 binaries. The old consumer is not rebuilt.

Each process completes four workspace lifetimes, with the first excluded from
timing summaries. All 56 lifetimes exercise form editing, direct list-row selection,
next-item selection, resize with retained native selection, 100 measured selection
updates, Review/Back navigation, restored model data and idempotent disposal.
Native factories/releases and component mounts/unmounts balance at two each per
lifetime. All 168 tracked host/editor weak references collect; cleanup policies
close and dispatcher shutdown finishes. Loaded framework hashes match the exact
build-output/package provenance, including the binary-substitution cases.

Both candidate full-list and virtualized renders were inspected for readable
forms/list/native notes, correct edited content and intact split layout. The check
uses real windows positioned offscreen and routed actions; it is not a physical
keyboard/IME/screen-reader campaign. Prior FW-005/006 acceptance keeps its scope.
The manually runnable sample is independent of GuidedGrade and contains no grading
or browser workflows.

## Representative measurements

These are three process pairs, each summarized by its three post-warmup cycles;
they are diagnostic measurements, not the framework's hosted performance gate.
The measured operation is 100 selection-driven updates over 100 descriptions.

- Full list: mount median 65.391 → 60.023 ms (−8.21%); update 91.059 → 96.106 ms
  (+5.54%). Mount UI allocation 4,743,664 → 4,743,128 bytes; update allocation
  unchanged at 17,086,728 bytes. Component builds stay at 100 per update batch.
- Virtualized: mount 59.205 → 58.278 ms (−1.57%); update 63.646 → 66.476 ms
  (+4.45%). Mount allocation 1,449,520 → 1,449,544 bytes (+24); update allocation
  15,592,984 → 15,570,584 bytes (−0.14%). Component builds remain 100.
- Candidate post-shutdown collected heap spans 1,533,728–1,552,328 bytes; private
  bytes 207,101,952–208,490,496; handles 401–403. Binary-upgrade runs end at 404
  handles with zero tracked survivors. These snapshots are not new memory budgets
  or proof that every operating-system resource is reclaimed.

The earlier complete campaign, before the explicit row-click check was added,
had full-list update −2.42% and virtualized update +1.09%. Preserve both: latency
varies, and these observations do not establish an update speedup or an absence
of small regressions. The final changes are within the existing 10% timing/2%
allocation reference thresholds, but this workload does not replace the full
accepted-baseline comparison. No library code, baseline or budget was changed.

## Reproduction and limits

Observed environment: Windows 11 x64 build 26200, SDK 10.0.402, .NET 10.0.13.
This is the sample's verified platform; other Windows versions/architectures and
future stable v1 packages need their own RC acceptance. Published alpha.7 does
not include subsequent uncommitted framework fixes in this checkout. This task
does not publish packages or certify the current checkout for release.

```powershell
pwsh -NoProfile -File tools/Test-WorkspaceAdoption.ps1 -OutputDirectory artifacts/new-workspace-adoption
```

The driver requires Python for its provenance/measurement summary and retains
temporary consumers/caches for inspection. Use fresh output directories. The
standalone README has direct restore/run/upgrade commands; ordinary sample use
does not require Python or the framework checkout.

The initial pilot incorrectly expected Application.Shutdown to finish without a
running application loop. A second raw-dispatcher pilot stalled and was stopped.
The final harness uses Application.Startup/Run with cleanup before Shutdown and
checks completion afterward. Both failed pilots and the intermediate successful
campaign remain preserved; no package fix was needed.

[Evidence archive](verification-evidence/2026-11-15-package-adoption/README.md)
contains reports, captured renders, source snapshots, exact package/assembly hashes,
lock files, restore/build logs and raw repeated results. The summary verifies
actual loaded libraries and unchanged old consumer binaries rather than trusting
a declared package version. FW-024 still owns reconciliation of the full alpha.7
publication and hosted-gate record; FW-019 does not assert that missing history.
