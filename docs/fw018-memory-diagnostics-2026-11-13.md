# FW-018: memory accounting and diagnostics

Status: in progress, November 13, 2026. This first continuation tightens evidence
validation; it does not change the framework runtime, sample workload, public API,
accepted performance baseline or release budgets.

## Reviewed ownership boundaries

The curated lifecycle/native-interop concepts and the generated analyzer neighborhood
lead to `MemoryWorkload`, `RetentionWorkload`, `LifetimeEditor`, `MemoryRow`, the memory
analyzer and its existing CI fixtures. Source review confirms that the application
adapter owns its 32-KiB native buffer and 16-KiB native-control payload. Each logical
row owns an 8-KiB payload. WPF realization and retained logical snapshots have
different lifetimes. The framework must release removed/disposed state; it must not
discard visited state merely to lower a memory measurement.

## Implemented evidence checks

[The analyzer](../tools/Summarize-NativeMemory.py) now validates all five retention
checkpoints, including report identity, row count, description count, required probes,
nonnegative counts, logical payload bytes and native buffer bytes. Native controls,
native payloads and popup owners must agree. Full visitation and half removal must
retain the expected logical state; empty lists and disposed hosts must retain none.
The existing native-realization bound (less than half the workload's rows) is now
also checked offline. This is a coarse correctness bound, **not a final v1 viewport
memory budget**. No new arbitrary process-memory threshold was introduced.

[Focused fixtures](../scripts/test-native-memory.py) pass. Eight new corrupt-evidence
cases cover independently meaningful accounting/retention failures; they do not add
application exception behavior. Both archived campaigns were reanalyzed successfully
(three runs each); only the longer campaign contains retention checkpoints, and only
that campaign receives checkpoint validation. No missing evidence is represented as
a pass. Its checkpoints remain 500 logical states/10 native controls after visitation,
250/10 after half removal, then zero after full removal and disposal.

Source evidence remains in the unchanged
[October 29 archive](verification-evidence/2026-10-29-native-window-memory/README.md).
Archive SHA-256: `d22a7328bce9a41725b4ced7290d952b20ad61bab8e034091022262c2cc69cfd`.
Local derived results are in `artifacts/fw018-retention-audit-2026-11-13/`.
This is retrospective analysis, not fresh runtime, full-suite or performance evidence.

## Remaining work, in order

1. Reporting isolation and the [bounded protocol](fw018-bounded-reporting-2026-11-13.md)
   are now implemented and verified. Earlier tools retained their samples; that
   historical evidence is preserved separately. Attribute the remaining process
   trends before budget calibration; do not reinterpret old measurements.
2. Establish workload-specific budgets for post-cleanup managed heap, owned native
   bytes, handles and realized controls using repeated fresh processes. Include
   multiple list sizes and the full-list comparison when making virtualization
   performance claims. Separate zero-survivor ownership invariants from noisy OS
   working-set/private-byte observations. Do not turn the coarse half-row check into
   a general claim that realization is bounded independently of item count.
3. Decide the smallest useful error diagnostic contract. Current `ViewHost` validates
   invalid descriptions and preserves original render/cleanup failures (including
   aggregate errors), but most validation errors lack an owning component/key path.
   Start with contextual failure reporting before an update inspector. Preserve
   exception evidence, avoid retaining view/control/delegate references in diagnostic
   records, and never silently recover or swallow errors. Any instrumentation needs
   enabled/disabled overhead measurements before adoption.
4. Rerun applicable correctness, ownership and paired performance checks for runtime
   changes, refresh both graphs, and record acceptance evidence before closing FW-018.

FW-010's standard WebView2 acceptance remains complete within its
[provider policy](native-provider-stall-policy.md). It does not establish a universal
browser memory ceiling. FW-018 stays open in the [framework backlog](backlog.md).
