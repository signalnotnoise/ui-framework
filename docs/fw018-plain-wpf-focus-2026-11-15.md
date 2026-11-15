# FW-018 plain WPF versus framework focus — November 15, 2026

The same fixed GDI increase reproduces without the framework. All eight scheduled
processes pass: two untouched controls and three focused pairs in alternating order,
covering 240 created test windows, 540 verified editor-focus acquisitions and 1,248
resource snapshots. No tracked editor, test window or content-root survivors remain
after cycle cleanup. GuidedGrade is absent at every process start/exit audit.

## Comparison and result

Both executables share window creation, activation, editor focus, text updates,
collection and resource reporting code. Plain references only WPF and verifies no
framework assembly loaded. Framework uses `ViewHost`, `VStack` and native views for
the same ten ordinary TextBoxes. Each process has one persistent anchor window and
30 successive test windows, with three anchor-to-editor focus rounds per window.
Every focus round verifies both active window and intended keyboard-focused editor.
Quiet controls require zero window activations. Both use .NET 10.0.13 / SDK 10.0.402.

Both quiet controls have 24 GDI objects after the first closed test window and 24
after the last. All six focused processes have 30 after the first and 30 after the
last; the final ten closed-window snapshots are uniformly 30. Thus the +6 GDI step
does not accumulate across 30 windows / 90 focus acquisitions in either build.

After UI-thread exit, both quiet controls have 14 GDI / 6 USER. All focused processes
have 17 GDI, with USER 7 or 8 in both implementations. The +3 residual GDI signature
therefore reproduces in independent plain WPF. The larger NativeLifetime workload
had different absolute counts (10/13); matching deltas are not proof of identical
native object identities. This comparison does not enumerate those objects.

The strongest supported explanation is bounded platform/input initialization for
this workload, rather than a framework-specific per-window GDI leak. Exact allocator
ownership remains unresolved. A shared WPF/Windows defect is not ruled out, and a
30-window plateau is not a universal bound. Framework and plain cases both show
heap/private-byte changes; no heap, handle or browser memory budget is approved here.

## Scope and limits

This deliberately omits virtualization, framework themes, browser controls, external
UI Automation queries and the optional COM cleanup policy. It establishes whether
the resource signature requires the framework; it does not replace the original
full-list/virtualization performance baseline or accessibility-peer reproduction.
The tracked weak references cover the ten editors, root and test window only.
Serializer/reporting overhead is shared; snapshots are not allocation-stack evidence.
The driver audits other application names at process start/exit, not continuously.

The first pilot incorrectly used `Activate()`'s return as the sole success criterion
even for an already-active anchor. It failed before a verified editor acquisition.
The corrected pilot and formal runs check actual active/focus state. Both pilots are
preserved outside formal totals. The successful pilot has 30 windows / 90 acquisitions.
No failed formal comparison was discarded or retried into the accepted set.

## Validation and next work

Both Release builds pass without warnings. The analyzer validates all expected phases,
cycle counts, focus counts, zero survivors, plain-build isolation and process audits.
Focused evidence checks reject missed focus, retained objects, a framework-loaded
plain case and a missing cycle. No framework library or accepted baseline changed;
this is diagnostic acceptance, not a latency/performance release gate. No fresh full
framework test-suite run is claimed for these sample-only changes.

FW-018 remains open for representative native/browser budgets, contextual failure
diagnostics and production accessibility peer-cache acceptance. Stop treating the
unidentified fixed +3 GDI count alone as evidence of a framework regression. Retain
the platform comparison and reopen deeper allocation attribution if the delta grows
with repeated work, differs from plain WPF or violates a representative budget.
Do not subtract it from raw counts or silently advance the accepted baseline.

See [sample and reproduction](../samples/FocusComparison/README.md),
[evidence archive](verification-evidence/2026-11-15-plain-wpf-focus/README.md), and
[preceding native attribution](fw018-guidedgrade-closed-resources-2026-11-15.md).
