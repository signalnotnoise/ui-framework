# FW-022 editor mount investigation — October 29, 2026

Status: attribution, untraced confirmation and local validation complete.
Source-first default adoption was subsequently authorized; see the adoption section below. This follows the preserved local editor mount failure during FW-007.

## Exact-commit hosted validation

[Validate run 40839920952](https://github.com/signalnotnoise/ui-framework/actions/runs/40839920952)
passed for `5fd4a2a1de9423baba0639d55f8b192d7e5dbff3`. All three jobs succeeded:
Windows correctness/API/package checks, graph checks and all 32 performance checks
against the unchanged accepted baseline `78c88fb`. Hosted editor mount measured
3290.98→3284.21 ms (-0.21%), with 3.76% fewer mount allocations. This is a different
machine and comparison baseline from the local alpha.6 failure; it does not erase it.
Downloaded hosted performance evidence and job metadata are preserved separately.

## Ownership and method

The editor workload creates 1,000 TextEditors inside AdaptiveGrid/Scroll and performs
50 unchanged updates. It owns its native presentation source and COM cleanup policy;
ViewHost must not create hidden windows or override application DPI/GC policy.
The original order constructs ViewHost before HwndSource. The diagnostic
`--editor-source-first` reverses only those two operations. Both operations remain
inside mount time and UI-thread allocations. During this initial experiment the default remained host-first.

`--trace-phases` opts into cleanup-policy, host-construction, source-creation,
native-attachment, theme-application, dispatcher, explicit-layout and disposal
boundaries. UI-thread CPU/allocation and process GC counts/pause totals accompany
wall time. GC pause totals are not UI-only time; CPU is quantized; neither should be
added to inclusive sampled-stack durations. Normal comparisons do not create a tracer.
Geometry verification is outside measured mount, only in traced diagnostics.

Three alternating measured pairs plus warmup provide phase attribution; separate
seven-pair untraced processes test end-to-end behavior. Both use the same binary,
SDK 10.0.402/runtime 10.0.13 and machine. Profiling is sequential and separate from
untraced timing. Workload, accepted revision, budgets and public APIs are unchanged.

## Supported bottleneck finding

Host-first construction leaves WPF HwndTarget process-DPI cache flags null while
creating the editors. Creating the source first initializes them to SystemAware
before construction. Mount geometry hashes and 1,000 native TextBoxes match across
all traced pairs. This is the same underlying fallback identified in FW-003, now
shown specifically inside editor construction.

Three-pair phase medians: host construction 1020.47→214.72 ms, with approximately
749 KB fewer construction allocations. Source creation costs 17.35→124.96 ms;
that additional early startup cost is included, not moved outside the timer.
Attachment and the dispatcher still each take roughly 0.6–0.7 seconds. Explicit
post-dispatcher layout is negligible; the dispatcher interval includes real work,
not just idle waiting. Theme application takes roughly 0.17–0.21 seconds.

The sampled profiles confirm the avoided call path: host-first construction has
84.58 ms of inclusive sampled spans under GetProcessDpiAwarenessFromWindow, versus
no samples in that fallback during source-first construction. In these profiles,
host construction UI CPU falls from 406.25 to 218.75 ms. Actual recorded contention
is zero in that phase; GC suspension overlap is approximately 10.40/9.53 ms.
Large Monitor.Enter/PollGC sampled spans are not equivalent to actual lock or GC
pause durations. The early-source profile itself has a long source-creation wall
interval with little UI CPU, reinforcing the need for untraced repeated totals.

This identifies avoidable startup work. It does not establish why the earlier
candidate mount median was 14.74% above its predecessor: both historical revisions
used host-first order. No claim is made that the entire difference was DPI lookup,
GC, contention or generic noise.

## Reproduction

Use the pinned local SDK, then build Counter in Release. Preserve each output path:

```powershell
./tools/Test-EditorMount.ps1 -TracePhases -Samples 3 -OutputDirectory artifacts/editor-mount-phases-new
./tools/Test-EditorMount.ps1 -Samples 7 -OutputDirectory artifacts/editor-mount-untraced-new
./tools/Collect-ComparisonTrace.ps1 -LayoutEditors -EditorHostFirst -OutputDirectory artifacts/editor-profile-host-new
./tools/Collect-ComparisonTrace.ps1 -LayoutEditors -EditorSourceFirst -OutputDirectory artifacts/editor-profile-source-new
python tools/Summarize-EditorMount.py artifacts/editor-mount-phases-new --output artifacts/editor-mount-phases-new/analysis.json
```

These are diagnostic initialization-order comparisons, not release gates. The
standard Test-Performance harness now uses source-first editors symmetrically.
The archived initial focused run used host-first on both sides.
Its overlay supplies the same current sample harness to both framework revisions;
it cannot independently measure instrumentation overhead versus the old sample.

## Seven-pair untraced initialization experiment

| Metric | Host-first median | Source-first median | Change |
| --- | ---: | ---: | ---: |
| Mount time | 2004.00 ms | 1835.14 ms | -8.43% |
| Mount UI allocation | 121,496,256 B | 120,767,376 B | -0.60% |
| 50 updates plus disposal | 19708.88 ms | 20102.22 ms | +2.00% |
| Update UI allocation | 1,113,312,416 B | 1,123,005,032 B | +0.87% |

Five of seven paired mounts favor source-first; paired changes range from -35.22%
to +11.01%. Work remains one mount body build and 50 update body builds. Report the
small update increases along with the startup reduction; this is not a uniform
speedup guarantee. The measured editor startup bottleneck is supported by cache
state, phase CPU/allocation, sampled stacks and a controlled ordering intervention.
The exact cause of every historical timing difference remains unresolved.

A follow-up can adopt source-first as an explicit editor benchmark protocol,
retain host-first as a cold diagnostic, and rerun the full four-workload accepted-
baseline campaign. This investigation does not silently change that default or
put native-window ownership into the framework. The reference application's main
window already initializes its handle before building its ViewHost (FW-003).

## Validation and preserved evidence

Local Test-Release passes: 159 framework tests, 15 visual checks, 21 stress
assertions, and startup/editing/disposal checks, with zero build warnings/errors.
The standard focused editor comparison against FW-007 passes 8/8 checks.
It overlays the current harness onto both revisions and the framework runtime is
unchanged: its -23.24% mount timing difference demonstrates variability, not an
additional optimization or an instrumentation-overhead comparison. Allocation
changes are +0.005% mount and +0.024% update; component work is unchanged.

Editor-analysis, timing-attribution and paired-comparison fixtures pass. Binary
hashes match across phase, untraced and profile runs; all 159 source-manifest files
match the working source. Geometry matches across profiles and repeated phases.
The accepted baseline and budgets remain unchanged.

[Raw evidence, profiles, source snapshot and SHA-256 manifest](performance-evidence/2026-10-29-editor-mount/README.md)
preserve the hosted run, local comparisons and validation separately. These new
diagnostics are local work; the hosted success applies to FW-007 commit `5fd4a2a`.

## Source-first default adoption

The user authorized adoption on October 29 after reviewing the investigation.
`source-before-editors-v1` creates HwndSource before ViewHost by default, on both
sides of Test-Performance. Native-source creation stays inside mount time and
allocation. The runner verifies protocol, source-first order and disabled tracing.
No baseline revision, budget, row count or update count changes.

`--editor-host-first` retains the previous cold diagnostic order;
`--editor-source-first` remains an explicit alias for the default. Supplying both
fails. Test-EditorMount now exercises the actual default for its source-first
condition and the explicit cold flag for host-first. Collect-ComparisonTrace
accepts `-EditorHostFirst` for cold attribution. Framework runtime/window ownership
and consumer startup are unchanged. The earlier evidence archives remain unchanged.

### Adoption validation results

The full seven-pair campaign against unchanged accepted revision `78c88fb` passes
**30/32** gates, so it is **not release eligible**:

| Metric | Baseline median | Candidate median | Change |
| --- | ---: | ---: | ---: |
| Themed mount | 5537.33 ms | 6309.20 ms | +13.94% — fails |
| Editor mount | 1953.94 ms | 2168.20 ms | +10.97% — fails |
| Editor updates | 21582.29 ms | 19564.27 ms | -9.35% |
| Editor mount allocation | 125471128 B | 120723008 B | -3.78% |
| Editor update allocation | 1156424600 B | 1112710280 B | -3.78% |

All component-work checks pass. Themed mount allocation is +0.07%; other time and
allocation gates pass. Paired editor mount changes range -25.23% to +22.97%, with
4/7 slower; themed mount ranges -20.84% to +39.86%, with 6/7 slower. These are
variable measurements, not evidence that either failed gate can be dismissed.
The themed execution path is unchanged by this editor-only ordering change.
The accepted-source comparison is not a measurement of the ordering intervention:
both sides now initialize source-first. The earlier same-binary experiment remains
the evidence for the startup-order improvement. Further attribution of these
accepted-baseline mount differences remains open before release.

Explicit default and cold phase checks verify source-before-host versus
host-before-source, identical geometry and 1,000 native editors. Conflicting flags
fail. All 159 measured source-manifest files match the working source.

[Adoption evidence](performance-evidence/2026-10-29-editor-source-first/README.md)
preserves this failed campaign separately; there was no retry-until-pass or budget
change. FW-022 remains open for the two failed mount gates.

After adoption, Test-Release passes 159 tests, 15 visual checks, 21 stress
assertions and startup/editing/disposal checks. No build warnings/errors.

## Hosted release gate decision

On October 29 the user designated GitHub Actions as the performance release gate.
The local 30/32 result remains a failed diagnostic comparison and is not erased.
A full passing hosted run for the new exact candidate commit will satisfy the
performance gate; an earlier FW-007 pass cannot qualify the changed protocol.
Correctness, packages and manual acceptance remain separate. Hosted validation
is pending; the backlog will record the exact commit, run and outcome after it
finishes. See [policy](performance-guardrails.md#release-gate-policy--october-29-2026).
