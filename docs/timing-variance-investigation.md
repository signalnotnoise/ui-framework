# FW-003 timing investigation

Reopened and resolved October 24, 2026 with a demonstrated DPI-initialization
bottleneck and tested fix. The inconclusive closure below is historical. See
[the causal investigation](timing-bottleneck-2026-10-24.md) and [backlog](backlog.md).

October 23, 2026. Investigation outcome: the timing increases were not sustained
across repeated comparisons; their precise cause remains unproven. No renderer
optimization is claimed. Existing failure evidence remains intact. This closes
the bounded investigation, not a guarantee of zero regression or v1 readiness.

## Method and results

Reviewed the original 533fbe1/native-style evidence (+7.66% virtualized and
+6.55% themed updates) and the later +28.86% full-list failure. The original
native-style production diff affects interop and theme code; the virtualized
workload does not construct native islands. This narrows possible execution
paths but does not prove the source of the measured increase.

New direct comparisons use pre-button-fix 13a5b98 and the current working tree,
including the already implemented compact-button fix and FW-016 build tooling.
Production framework C# is unchanged during this investigation. These fresh
comparisons do not pretend to rerun the original 533fbe1 revision.

All three campaigns use Test-Performance.ps1, the same machine, SDK 10.0.402,
runtime 10.0.13, 1,000 rows, 50 operations, one discarded warmup per side and
seven alternating measured processes per side in all four scenarios. No power,
affinity, runtime-policy or budget adjustment was made. Native realization,
workloads and correctness requirements were retained. Profiles were collected
separately from timed campaigns. The machine was not reserved exclusively for
benchmarking, which limits environmental attribution.

| Campaign | Scenario | Mount time | Update time | Mount UI bytes | Update UI bytes |
| --- | --- | ---: | ---: | ---: | ---: |
| direct | full-list | +0.25% | -1.23% | -0.07% | -0.53% |
| direct | virtualized | +1.07% | -3.24% | -0.00% | +0.00% |
| direct | themed-full-list | +26.48% | +117.30% | -0.10% | -1.55% |
| direct | layout-editors | -1.60% | +1.82% | +0.01% | +0.89% |
| control | full-list | +5.82% | -2.83% | -0.08% | +0.08% |
| control | virtualized | +4.52% | +8.06% | +0.00% | +0.00% |
| control | themed-full-list | -4.00% | -13.48% | +0.01% | +0.57% |
| control | layout-editors | -0.10% | +1.15% | -0.00% | -0.89% |
| repeat | full-list | +5.91% | +2.66% | +0.02% | +0.34% |
| repeat | virtualized | +1.55% | +2.71% | -0.00% | +0.00% |
| repeat | themed-full-list | -1.33% | -1.67% | +0.02% | +0.01% |
| repeat | layout-editors | -1.38% | -0.47% | +0.00% | -0.00% |

The first direct run FAILED two of 32 gates (themed mount/update). Its 117.30%
update increase remains a failed result. The full repeat passed all 32 gates;
this does not retroactively turn the first run into a pass. The identical-binary
control also passed 32 budgets but is explicitly ineligible for release.
All component build and mount/unmount medians matched in all campaigns.
Raw per-process results, min/max ranges and descriptive paired changes are in
[the evidence directory](performance-evidence/2026-10-23-timing-investigation/).

The control uses SHA-256-identical Counter/core/WPF assemblies and runtime
configuration at two paths. It produced +8.06% virtualized update time and
-13.48% themed update time despite identical code. Its archived setup baseline
revision was not executed: both control sides execute the candidate payload.
Binary manifests identify the actual payloads. Control differences are measured
variation, not implementation gains. The original +28.86% full-list flag did
not reproduce in either fresh direct campaign (-1.23%, then +2.66%). Themed
results changed from +117.30% to -1.67%, with unchanged component work and near
zero allocation difference on the repeat. These observations prevent assigning
one source-code cause or claiming a reliable speedup from these medians.

## Profiling and attempted improvement

Pinned dotnet-trace 10.0.745401 collected sampled thread time plus runtime events
on both themed revisions. Aggregate reports mix UI and finalizer threads and
waits; their percentages are not CPU-consumption evidence. Inspecting the main
thread separately and attributing CPU_TIME/UNMANAGED_CODE_TIME spans to their
nearest managed frame showed approximately 39–44% in WPF's DPI-awareness lookup
context. The call path includes SystemResources.EnsureResourceChangeListener.
This is a whole-process observation, not a phase-specific causal attribution.

A separate source-copy prototype created a hidden HwndSource before the host
and attached its root. The same prototype assembly, with/without the flag,
retained 1,000 rows, 50 operations and matching component work. One profiled
comparison changed mount/update from 10,577/12,602 ms to 10,233/8,666 ms, and
DPI lookup left the leading main-thread frames. This is exploratory evidence
for presentation context affecting the measurement. A single instrumented pair
cannot establish repeatable improvement or explain the initial failures.
The prototype is preserved; it was not adopted into production or substituted
for the default release workload. Validate any future presentation-context
change with repeated matched comparisons and geometry/DPI checks first (FW-004
representative native hosting and FW-019 reference-workspace acceptance).

Profiles, nearest-managed-frame thread summary, preparation script, trace hashes
and compressed raw traces are under the evidence profiles directory. Startup,
mount, updates and teardown are all included. Finalizer samples can reflect
waiting; no GC/runtime tuning is justified by their aggregate prominence alone.

## Measurement tooling

Test-Performance now provides an opt-in -IdenticalBinaryControl. It clones the
candidate output, verifies executable/config hashes and never replaces original
build outputs. Controls always have releaseEligible=false. Ordinary gates retain
the accepted 78c88fb baseline and every existing budget. Failed ordinary runs
remain failed even when a control is variable or a later repeat passes.

Binary manifests and whole-process CPU/wall telemetry accompany raw runs.
Process CPU includes other threads and startup/teardown; UI allocation metrics
remain the existing per-thread workload measurements. Source capture includes
.editorconfig and PublicAPI text files so FW-016 inputs are reproducible.
Post-processing Python sources are included in future captures as well.

Summarize-PerformancePairs.py records each measured pair's percentage, median,
range and positive-pair count. It excludes warmups and rejects incomplete pairs,
invalid metrics and mismatched workloads/runtimes. These are descriptive
statistics, not confidence intervals or replacement pass/fail rules. The focused
fixture runs in CI alongside the existing tooling checks.

## Resolution and operating rule

Close FW-003 as an investigation with an inconclusive exact causal attribution.
No reproducible renderer hotspot was isolated that justified a correctness-safe
runtime optimization. The presentation prototype remains a measured candidate,
with its limited evidence stated explicitly. No baseline or budget is promoted.
On a future timing failure: retain the failed run, compare actual binary/source
manifests, run an identical-binary control, profile separately and retain a full
matched repeat. Reopen a focused investigation for a sustained source-related
increase; a noisy control does not exempt a release from its existing gates.

Validation passed: zero-warning Release build, 119 correctness tests, 15 visual
checks, 21 stress assertions, paired-diagnostic fixtures and curated/generated
graph checks. Evidence includes the TRX, stress report and complete validation log.
