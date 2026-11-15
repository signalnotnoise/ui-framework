# Bounded reporting — November 13, 2026

FW-018 remains in progress; budgets are not set. The native and browser memory
samples now use `bounded-sample-files-v1`. Framework runtime and the accepted
performance baseline are unchanged.

## Reporting contract

[BoundedReporting](../samples/MeasurementTools/BoundedReporting.cs) writes each sample
to its own new JSON file and closes the file before returning. It retains no strong
sample reference, no growing sample list and no growing weak-reference list. One
weak probe is kept until the next existing workload collection point; the sample
must be collected before another is recorded. Completion verifies the final sample
before loading any report data. Both callers construct samples in non-inlined
methods so construction temporaries cannot root them in the measurement loop.

This bounds historical reporting retention by sample count. It does not promise a
fixed number of bytes for arbitrary sample contents: serialization buffers depend
on the largest individual sample, and serializer caches, OS file caching and other
fixed measurement state remain. Serialization/file writes occur outside workload
timers, but can still influence subsequent GC and process trends. Natural-heap
readings can include garbage awaiting collection. This is not a latency comparison.

After measurement, completion reloads all files to preserve the existing `Samples`
report schema. That reconstruction is deliberately unbounded and must not be
included in a sustained-workload heap budget; after-dispatcher snapshots can include
the reconstructed report. All raw files remain available even if a later check
fails. Existing browser `retention-*.json` evidence remains available as well.

The [native analyzer](../tools/Summarize-NativeMemory.py) requires the campaign's
declared protocol, exact verified-sample count, zero survivors and one pending weak
probe. Older retained-reporting archives remain readable under their original
protocol. The browser driver requires the same bounded guarantees. A missing probe
or unverified sample fails validation; it is not silently interpreted as zero.

## Controls and campaign evidence

The [standalone reporting checks](../tests/ReportingRetentionChecks/Program.cs) verify
128 samples, collection between writes, every payload and sequence number restored,
and a deliberately retained sample detected and then released. The legacy retention
controls remain. The one-batch native regression also passes. Analyzer fixtures
reject missing/wrong protocol and incomplete sample verification.

Three fresh native processes pass the unchanged workload: 3 warmup plus 60 measured
batches, four workspace cycles per batch, 500 rows. Each verifies 63 collected
reporting samples. Across runs, 756 workspace cycles and 57,456 native mounts
including warmup retain zero tracked application objects/owned native bytes after
batches; shutdown and logical-state checkpoints pass.

Measured first-to-last collected-heap changes are **+46,904 / −4,648 / +44,504 bytes**.
Private-byte changes are +3,411,968 / +2,969,600 / −1,773,568; handles change +1 each.
These observations remain too variable to establish a budget. Previous retained
reporting produced roughly 74 KB of heap growth in those runs, but the protocols
were not alternated in a paired experiment. Do not subtract the earlier reporting
delta or describe the new figures as a framework memory optimization. Framework
source hashes match the prior campaign; exact measured sources are retained.

All six browser runs pass: three fresh-profile and three reused-profile processes,
each with four warmup and 40 measured cycles. All 264 reporting samples were verified
collected. Original parent/popup ownership, process-exit, deliberate-root and shutdown
checks remain enabled (270 parent/popup cycles including negative controls).

Collected-heap changes by repetition are +92,256 / +99,384 / +91,184 bytes for fresh
profiles and +216 / +61,688 / +58,688 for reused profiles. Handle changes are +9 / +22 /
+3 fresh and +3 / +23 / +8 reused. Those residual trends need attribution before
budgets. Fresh-profile heap growth is not consistently lower than earlier retained
reporting. The earlier six-run campaign observed +34,968 to +103,496 managed bytes
and +38 to +53 handles; neither campaign is an alternated protocol comparison or
proof of a framework regression/improvement. Browser processes and tracked owners
exit/collect, but this does not prove whole-process leak freedom.

All 190 native raw samples (including its deliberate-root case) and 264 browser
spooled samples match the reconstructed reports exactly. Browser legacy per-cycle
files also match. Release validation passes 172 tests, 15 visual checks, 21 stress
assertions, reporting controls, the one-batch regression and window/shutdown checks.
Analyzer fixtures pass. Raw data, exact sources, manifests and logs are
[hash-archived](verification-evidence/2026-11-13-bounded-reporting/README.md).
Both knowledge graphs are refreshed and checked. No paired latency or hosted
performance gate was rerun: framework runtime is unchanged; no speedup is claimed.

To reproduce in a PowerShell 7 session with the recorded .NET SDK/runtime, use fresh
output directories (do not invoke the array-valued browser modes through `pwsh -File`):

```powershell
./tools/Test-NativeLifetime.ps1 -Batches 60 -OutputDirectory ./artifacts/native-bounded-new
./tools/Test-WebViewAcceptance.ps1 -Modes @('retention-fresh','retention-reused') -Repetitions 3 -OutputDirectory ./artifacts/browser-bounded-new
```

These checks open their own test windows with local pages and isolated browser
profiles. They do not use user browser profiles or perform interactive input tests.

## Remaining FW-018 work

Calibrate representative budgets using this explicit protocol, repeated fresh
processes, additional row counts and process/handle observations. Keep collectability
and owned-native-byte invariants separate from GC/OS memory trends. Contextual error
diagnostics and any enabled/disabled instrumentation overhead remain open. See the
[backlog](backlog.md) and [preceding reporting investigation](fw018-reporting-retention-2026-11-13.md).
