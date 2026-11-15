# Isolating retained reporting data — November 13, 2026

FW-018 remains in progress. This change instruments the measurement tools only;
the core and WPF runtime, workload sizes, accepted baseline and performance budgets
are unchanged. It does not claim an application memory or latency improvement.

## Method

Both native and WebView2 retention campaigns append completed telemetry objects to
a `List<object>`. Earlier samples, their nested dictionaries/arrays and the list's
backing array therefore remain reachable during later measurements. Preallocating
list capacity alone would not remove the retained sample graphs.

[ReportingRetention](../samples/MeasurementTools/ReportingRetention.cs) implements
`release-reporting-roots-v1`, shared by the two sample projects, outside the framework:

1. After all workload measurements, serialize the samples to a new evidence sidecar.
2. Track sample roots weakly; collect and measure managed memory with reporting roots
   still present. Serialization caches are initialized before this measurement.
3. Clear the list and its capacity; use the same collection sequence and measure
   again. Count surviving sample roots.
4. Reload the samples **after** measurement to preserve the report's existing shape.

No sample is removed during the workload. Old batch trends and ownership checks keep
their meaning. The signed before/after delta is an observation of released reporting
roots, not an exact object-size sum or an adjusted batch slope. GC/finalizer activity
can affect it. Weak-reference bookkeeping remains alive across both readings. Fixed
measurement state, serializer caches, the native counter's reusable tracking capacity,
browser negative-control evidence and other application/runtime caches are outside
this release probe. Process working set/private bytes are not measured by this probe.

The browser clears its final async local references before the probe so they do not
independently root the last sample's nested metadata. No browser instance, process,
profile or application state is forcibly released to improve the result.

## Verification

The [standalone controls](../tests/ReportingRetentionChecks/Program.cs) verify an empty
list, a deliberately retained sample (exactly one survivor), release of all unrooted
samples and lossless report payload restoration. They are wired into
[Release validation](../tools/Test-Release.ps1). The initial empty control showed an
8,464-byte GC delta, reinforcing that deltas must be interpreted with uncertainty.
The eight-sample 32-KiB payload control released 262,704 observed bytes and all roots.
These controls test the instrumentation; they are not framework memory budgets.

The native campaign completed three fresh processes, each with 3 warmup and 60
measured batches, 4 workspace cycles per batch and 500 rows. Each run released
**31,808 observed bytes** from 63 samples (list capacity 64), with zero surviving
sample roots. All ownership/shutdown checks passed: 756 workspace cycles and
57,456 native mounts including warmup, with zero tracked application survivors or
owned native bytes after batches. First-to-last collected heap changes were
74,712 / 74,640 / 74,712 bytes. The reporting delta is not the entirety of that growth,
and end-of-run measurements cannot be subtracted from those trends.

The first browser pilot correctly failed isolation: one reporting sample remained
reachable (36,480 observed bytes released). Browser owner/control/process retention
checks still passed. Explicitly assigning the loop's sample local to null did not
establish collection. Sample construction and serialization now run in a separate
non-inlined method to remove the caller's temporary sample root. The failed pilot
and its exact source are retained, not replaced. All six corrected browser runs pass: three fresh-profile runs each release 38,016
observed bytes, and three reused-profile runs each release 25,344 bytes. Every run
collects all 44 sample roots. All original browser ownership, process-exit,
negative-control and shutdown checks remain enabled. Including the negative
controls, these runs cover 270 parent/popup cycles and 540 one-time releases.

Source snapshots are matched to campaign manifests. After the native campaign,
the CLI's report directory creation was moved earlier so direct memory invocations
can create the new sidecar in a fresh nested directory; this does not change the
measured workload. The initial one-batch direct invocation created the directory but retained its lone
sample root (80-byte observed delta). This is a failed isolation pilot, despite the
old process reporting success. The native sample now also creates telemetry in a
non-inlined method. Both sample callers fail explicitly on surviving reporting
roots, in addition to driver/analyzer checks. The one-batch case is now in Release
validation and passes with zero sample survivors (224 observed bytes).

A final three-process native campaign with the corrected construction/guard passes
all 60 measured batches per run: reporting deltas are 31,808 / 31,832 / 31,808 bytes,
with zero roots surviving. Two final browser guard checks also pass (fresh 38,016,
reused 25,344 bytes, zero roots). Earlier six-run browser measurements and failed
pilots remain preserved alongside these final-source runs. None of the failed
pilots is counted as successful isolation.

Final Release validation passes 172 tests, 15 visual checks, 21 stress assertions,
reporting controls and actual-window ownership/shutdown. The direct fresh-directory
memory invocation also passes. Analyzer fixtures pass. Raw reports, failed pilot,
measured source snapshots and verification logs are
[preserved with per-entry hashes](verification-evidence/2026-11-13-reporting-retention/README.md).
Both knowledge graphs are refreshed and checked separately. No full paired latency
benchmark or hosted performance gate was rerun: framework runtime is unchanged and
this probe runs after workload measurements, with no speedup claim.

## Next budget work

The [bounded reporting continuation](fw018-bounded-reporting-2026-11-13.md) now
implements and verifies that protocol. Residual heap/handle attribution and budget
calibration remain. Keep both protocols explicit and
repeat on the same workload/runtime; do not silently reinterpret old runs or subtract
this end-of-run delta from each batch. Then establish workload-specific limits for
retained logical state, realized native controls, post-cleanup heap and handles.
No memory budget is approved by this change. Contextual error diagnostics remain a
separate open part of [FW-018](backlog.md).
