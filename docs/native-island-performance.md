# Native island performance — October 24, 2026

This FW-004 record predates the subsequent FW-003 causal finding. Its archived
measurements retain their original setup. The current standard renderer harness
uses explicit presentation initialization; see [cause and protocol change](timing-bottleneck-2026-10-24.md).

FW-004 measures the existing native style boundary; it does not change renderer
behavior, accepted performance budgets or the default release workloads.

## Workload and ownership

The opt-in Counter workload creates 32 keyed native workspace islands. Each
contains a search TextBox, action Button, CheckBox and four-entry TreeView.
Every fourth island has an island-local Button style and an explicit TextBox
style. The WPF host owns attachment, the style boundary and release callbacks;
the sample owns these native contents. Core acquires no WPF dependencies.

A hidden 1320×1080 HwndSource is created before mounting. Fifty operations run
in five batches of ten: retained content updates, outer theme replacements,
switching the scope, restoring the initial scope, then alternating removal and
remounting of half the islands. The measured update interval includes dispatcher
flush and layout, excludes validation assertions and final disposal. Mount
includes initial host construction/theme application/layout, excludes HwndSource
construction. Phase timings and UI-thread allocations are retained individually.
The scope changes once at operation 21 and once at operation 31; each of those
batches includes nine further retained updates. Their timings are batch totals,
not per-switch costs. The final batch performs five remove/remount cycles.

Assertions verify retained input identity, text/selection, explicit and local
style precedence, detachment before release and exact factory/release balance.
Each process performs one root build at mount, 50 root rebuilds during updates,
32 initial native creations, 80 update creations/removals and 112 total releases
after disposal. `MountBodyBuilds`/`BodyBuilds` count the root description factory;
`Mounts`/`Unmounts` count native factories/releases in this workload. There are
no child Components here; these counters are not comparable to Dashboard's
component counts. They establish equal work between the two native runs.
Balanced callbacks do not establish garbage collectability, asynchronous
initialization safety, IME preservation or screen-reader acceptance; those
remain the separate FW-005/006/010/018 contracts.

`Inherit` and `PreserveNative` intentionally produce different style policies.
Their timing difference is workload-specific scope cost/benefit, not a renderer
optimization or proof of equivalent visual output. Both modes temporarily switch
to the opposite scope in batch three, then restore it in batch four.
Fresh-process resource initialization is included when the process first enters
`PreserveNative`: at mount in preserve-first mode, in batch three in inherit-first
mode. Phase differences include that intended initialization placement.

## Reproduction

Use the repository's pinned .NET SDK on Windows. Build once, then run sequentially:

```powershell
dotnet build samples/Counter/Counter.csproj -c Release -warnaserror -p:NativeIslandBenchmark=true
pwsh -NoProfile -File tools/Test-NativeStyleIsolation.ps1 -Samples 7 -OutputDirectory artifacts/native-scope-new
pwsh -NoProfile -File tools/Test-Performance.ps1 -BaselineRef fbf5204051fa15da420a4917c2b1fe15fc746a78 -Samples 7 -IncludeLayoutEditors -IncludeNativeIslands -ReportOnly -OutputDirectory artifacts/native-revisions-new
```

Each script refuses an existing output directory, discards one process per side
as warmup, alternates order, retains raw JSON/logs and hashes the executed output.
The scope-only comparison uses exactly one binary. The revision comparison
overlays the same candidate harness onto both revisions and retains the four
existing workloads alongside both new scenarios. Native rows are 32; existing
workloads remain 1,000 rows and 50 operations.

The native harness is compiled only with `NativeIslandBenchmark=true`; the
accepted baseline predates native interop. Ordinary performance runs build it
out and retain their original workload and release eligibility rules. Supplemental
native runs require a revision supporting the existing native scope API and are
always marked ineligible for release. They cannot replace accepted-baseline gates.

## Presentation-context experiment

The [FW-003 prototype](timing-variance-investigation.md) is a separate artifact
copy of the themed Dashboard workload. Repeat its single binary in attached and
unattached modes using `tools/Test-PresentationContext.ps1 -PrototypeDll <path>`
with a fresh output directory and seven samples. This changes presentation
context, not framework implementation. It must not replace release workloads
without geometry, DPI and representative visible-window acceptance.

## Evidence

Measured on the same Windows machine with SDK 10.0.402 and runtime 10.0.13,
seven measured fresh processes per side plus one discarded warmup. Processes
and campaigns run sequentially, without profiling during timings. The machine
was not exclusively reserved, so environmental variability remains possible.

### Same-binary native scope comparison

Negative percentages mean preserve-first mode used less time or allocation.
All seven pairs favored preserve-first for mount time, total update time,
total allocations and outer-theme replacement. Retained-update timing favored
it in five of seven pairs. The transition batches deliberately go in opposite
directions in the two modes; their differences include different style policies.

| Phase | Inherit time (ms) | Preserve time (ms) | Time change | UI allocation change |
| --- | ---: | ---: | ---: | ---: |
| Mount | 331.60 | 306.86 | -7.46% | -12.26% |
| All 50 updates | 922.86 | 536.93 | -41.82% | -20.36% |
| Retained updates | 26.24 | 24.44 | -6.87% | -0.39% |
| Outer theme replacement | 334.29 | 86.46 | -74.14% | -47.38% |
| Switch to opposite scope | 87.34 | 70.25 | -19.56% | -1.58% |
| Restore initial scope | 77.39 | 68.23 | -11.84% | **+2.73%** |
| Remove/remount | 396.29 | 288.17 | -27.28% | -6.63% |

Update UI allocations were 110,650,536 versus 88,117,504 bytes. Factory,
release and root-build work matched in every pair. These results support using
the existing opt-in boundary when native styling is desired; they do not justify
changing the inherited-style default or claiming a new renderer optimization.
Scope restoration has a measured allocation tradeoff even though the total is
lower. No runtime source was changed.

### Alpha.5 revision comparison with all existing workloads

The same candidate harness ran against `fbf5204` and the working tree.
All work-count checks matched. All allocation changes were below 0.1%.
The campaign failed one of 48 checks: full-list update time. The complete
failed run is retained; the native results cannot clear this failure.

| Workload | Mount time change | Update time change | Update UI allocation change |
| --- | ---: | ---: | ---: |
| Full list | -9.73% | **+43.29% — failed** | -0.02% |
| Virtualized | -7.08% | +0.79% | +0.01% |
| Themed full list | +1.18% | +5.49% | +0.05% |
| Layout/editors | +0.84% | +3.16% | approximately 0% |
| Native inherit | -0.19% | +1.58% | approximately 0% |
| Native preserve | +3.68% | +1.20% | approximately 0% |

Both native revision cases stayed within existing budgets. Their small timing
differences do not establish an implementation improvement. The unthemed
full-list flag requires the separate identical-binary control below. This
supplemental run was ineligible for release regardless of its result.

### Full-list identical-binary control

A separate seven-pair control ran copied candidate assemblies with verified
identical hashes. It used the full unchanged 1,000-row/50-operation workload.
Apparent mount time changed -11.87% and update time -26.41%, with approximately
+0.04%/+0.10% UI allocation changes and identical component work. Update medians
were 13,289.82 versus 9,780.64 ms; individual measured samples ranged from
9,910.03–18,269.26 ms on one side and 8,334.06–18,088.45 ms on the other.

That is variation, not an optimization. The control is ineligible for release
and cannot retroactively pass the +43.29% revision flag. No runtime source
change explains that flag; its exact cause remains unassigned. Do not publish
based on this task's results. A future release still requires its ordinary
accepted-baseline campaign, plus resolution of any reproducible increase.

The control setup also successfully built the accepted alpha.1 source with the
current ordinary harness and native feature disabled. This verifies build
compatibility, not alpha.1 runtime performance: both measured sides ran the
candidate payload. Its snapshot and manifests identify that distinction.

### Seven-pair presentation-context repeat

The unchanged FW-003 prototype was run without a profiler, alternating attached
and unattached fresh processes. It retained 1,000 rows, 50 operations and equal
component work/runtime in every pair. Unlike the native-island workload, this
prototype includes HwndSource construction in its mount timer. Do not compare
the two workloads' absolute mount costs.

| Metric | Unattached median | Attached median | Change |
| --- | ---: | ---: | ---: |
| Mount time (ms) | 7,177.38 | 8,636.60 | **+20.33%** |
| Update time (ms) | 10,219.02 | 6,953.05 | -31.96% |
| Mount UI allocation (bytes) | 328,069,680 | 536,371,616 | **+63.49%** |
| Update UI allocation (bytes) | 458,470,680 | 475,834,056 | **+3.79%** |

The update benefit repeated, but with material startup/allocation costs. This
supersedes the single profiled pair as the stronger description of this
experiment. The prototype is **not adopted** as an optimization or replacement
release workload. It changes presentation context and has not established child
geometry, DPI or visible-window acceptance. FW-019 can evaluate representative
application startup/steady-state boundaries separately. No DPI, GC, COM policy,
accepted revision or budget changed here.

### Archived evidence and validation

The [evidence archive](performance-evidence/2026-10-24-native-islands/) retains
raw JSON/logs, manifests, revision and scope summaries, paired variation,
source snapshots, final tools and the prototype source. `revision-source.zip`
is the exact initial measured source: the later compile-time feature guard,
per-scenario row metadata and focused-selection tooling are preserved separately
in `control-source.zip` and the final tool copies. The native workload body is
unchanged. The guarded default harness successfully built the older accepted
source; seven-pair same-binary control ran with the guarded candidate build.

Final correctness validation passed: a zero-warning Release build, all 119
regression tests, 15 visual checks and 21 stress assertions. Paired-analysis
fixtures (including mixed workload row counts), PowerShell syntax checks and
non-evidence whitespace checks passed. Curated and generated graph verification
passed; graph generation uses the uncommitted worktree. FW-004 is complete as
a measurement task; performance release clearance is not established.
Changes remain uncommitted; no package version or publication changed.

See the running
[backlog](backlog.md), [native ownership contract](native-interop.md) and
[performance guardrails](performance-guardrails.md).
