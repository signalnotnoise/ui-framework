# FW-010 browser resource retention — November 11, 2026

## Review before adding tests

Reviewed the existing WebView2 lifetime, failure/recovery and shutdown cases, plus
the controlled native async-lifetime and real-window shutdown checks. Existing
exception cases exercise distinct resource contracts: partial initialization must
release accepted resources; late faults must be observed after removal; timeout
must preserve pending ownership; an invalid profile/missing runtime must allow a
fresh-owner recovery. They do more than assert an exception type. Keep these cases;
no additional argument/null/range/unknown-mode exception matrix was added.

Two WebView2 aggregate checks previously accepted the expected exception among any
number of other errors. They now require exactly one flattened error, identical to
the expected initialization/late-fault instance, so extra cleanup errors fail.

The two new modes (`retention-fresh`, `retention-reused`) exercise successful normal
lifecycle, not injected crashes or exception handling. A deliberately rooted closed
owner is the negative control: the weak-reference probe must see it alive and then
see all its tracked resources collected after that root is cleared.

## Workload and ownership boundary

Each mode runs 44 parent/popup create/load/close cycles: four warmup and 40 measured,
plus one negative-control pair. Each child loads a real local page and executes a
title query. Fresh mode uses a new profile per cycle; reused mode uses one profile,
waiting for all captured processes to exit before the next cycle. Three repetitions
per mode run in separate processes. No personal profile, external site, keyboard
input, process killing or benchmark-budget change is involved.

For each pair, require exactly-once parent/child host release and drained deferrals.
Capture OS handles for processes returned by that isolated environment before close,
then require the matching BrowserProcessExited event, normal exit of every captured
process within 30 seconds and an empty environment process list. Handles preserve
process identity across PID reuse and are disposed after checking.

After returning from the separate non-inlined async cycle method, use a fixed three
passes of dispatcher drain, GC/finalizers and dispatcher drain. Weak references to
each owner, window, ViewHost, WebView2 and core, plus the shared environment, must
all be dead. This separates application roots from the measurement's local frame.
The deliberately retained owner must remain detectable before its root is cleared.

Per-cycle JSON records live browser-process memory; natural and collected host heap,
private bytes, working set, handles, GDI and USER counts; UI-thread allocation and
elapsed time; process IDs/exits; release counts and any surviving weak references.
Elapsed time includes browser startup/navigation/exit. It is not renderer latency.
The harness retains telemetry rows, so managed-heap drift includes measurement data.
Profile disk caches persist intentionally. Process-private bytes and heap trends
are observations, not representative memory budgets or proof of universal leak freedom.

## Validation

The short pilot passed both modes (eight cycles each).
The combined campaign passed all 48 cases (including 42 prior cases with the
stricter exception assertions), but exposed a reporting mismatch: its loop ran 44 cycles while the
summary said 24. Per-cycle files and the original tested-source archive preserve
what actually ran. The final code uses a single CycleCount constant for the loop
and report; the same 44-cycle workload passes all six final fresh-process runs.
Final report counts were cross-checked against all 44 saved samples in each run.

Final campaign totals (including six negative controls): **270 parent/popup cycles,
540 browser owners, 1,350 captured process exits and 2,970 tracked weak references**.
Every owner releases exactly once; every captured process exits normally; all
tracked references are dead after the fixed collection protocol. All six negative
controls retain the parent owner while deliberately rooted, then release all
tracked references. There are 240 measured cycles after 24 warmups, plus the six
negative controls. Runtime 157.0.4341.42, SDK 1.0.4314.35, .NET 10.0.13.

Across the six runs, first-to-last measured collected-heap change ranges from
-7,768 to +80,704 bytes. Host private bytes decline by 4,448,256–7,602,176 bytes.
Host handle deltas range from +3 to +27 and USER deltas from 0 to +2; GDI deltas
are zero. Inspection of all per-cycle traces shows early rises followed by plateaus
or drops, rather than per-cycle growth. One run has a later handle step and partial
drop. These observations do not identify the native handle owners or establish an
unlimited-duration bound. Raw traces remain evidence for FW-018 budget work.
The harness retains telemetry rows, which contribute to managed-heap drift.

The final six retention cases supplement the 42 existing cases that passed in the
combined campaign. Full Release regression passes 172 tests, 15 visual checks, 21 stress assertions
and actual-window startup/editing/disposal and native-window lifetime checks.
[Raw logs, manifests and source snapshots](verification-evidence/2026-11-11-webview2-retention/README.md)
preserve the preliminary reporting mismatch and the corrected final results.

The new tests and assertion changes are confined to the acceptance sample; framework
runtime and GuidedGrade code are unchanged. No new Test-Performance comparison is
claimed. Hosted release gating remains tied to an exact committed candidate.

## Remaining scope

Retention evidence alone does not close FW-010: repeated interactive/pointer/clipping
coverage and the explicit support boundary for a provider that never settles remain
open. GuidedGrade adoption is separate application work. Memory budgets remain under
FW-018; these local-page measurements do not establish budgets for arbitrary websites.
