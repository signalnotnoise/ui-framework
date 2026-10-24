# Reopened timing bottleneck investigation — October 24, 2026

FW-003 was reopened and completed October 24 with a supported initialization
bottleneck finding, tested fix and passing local performance/correctness gates.
No subsequent backlog item was started during this work. The prior investigation
recorded variation but did not identify its cause.
Neither a noisy control nor the native-island improvement clears the preserved
+43.29% full-list failure. See the [backlog](backlog.md),
[historical investigation](timing-variance-investigation.md) and
[native/presentation results](native-island-performance.md).

## Ownership and method

Core state mutation notifies retained component sessions; WPF ViewHost schedules
reconciliation at DataBind priority. The sample flushes through ApplicationIdle,
then requests layout. Diagnose these boundaries before changing renderer code.

Counter's optional `--trace-phases` records mutation, dispatcher and layout
intervals for all 50 unchanged operations, plus initial rendering and layout.
It records monotonic timestamps, UI-thread user/kernel CPU from GetThreadTimes,
UI-thread allocations and process-wide GC collection counts. The latter do not
identify a collection's exact pause duration. CPU accounting is quantized and
does not measure other threads. EventSource phase markers permit separate
sampled-stack/runtime-event correlation using `SignalNotNoise-Comparison`.

This tracer is constructed only with the explicit diagnostic flag. Traced
measurements include observer overhead and are not release gates or evidence of
an optimization. Default workloads, accepted source revision and budgets remain
unchanged. Preserve raw runs and correlate fast/slow operations, not only totals.

## Current evidence

Four initial diagnostic full-list runs locate essentially all update time in
the dispatcher/render interval; explicit post-flush layout is negligible.
Compact/expanded row changes and filtering dominate that interval. UI-thread
CPU is high, so this is not explained by generic idle waiting.

Inspection of the **installed** PresentationCore/PresentationFramework 10.0.13
assemblies using ILSpy 9.1.0.7988 identifies a concrete path:

1. SystemResources resource lookup ensures a resource-change listener.
2. Its process-DPI getter checks nullable HwndTarget cached flags.
3. Before a presentation target exists, those flags remain null; the fallback
   calls DpiUtil.GetProcessDpiAwarenessFromWindow, opens a process handle, queries
   DPI awareness and closes that handle repeatedly.
4. Creating an empty HwndSource initializes the cache through WPF's normal
   HwndTarget initialization. Disposing that source leaves the process cache set.

Three isolated tests made 10,000 reads through the same installed getter in each
condition. Cold reads took 109.61–124.29 ms and allocated 1,041,920 bytes; primed
reads took 0.35–0.45 ms and allocated 240,000 bytes. Cache observations changed
from null flags to SystemAware; the reported awareness was SystemAware before
and after. Only public presentation construction changes state; reflection reads
the private properties for diagnosis and never writes them.

Initial separate UI-thread profiles place 4.23 seconds of sampled time under
the fallback before priming and no recorded samples there afterward. These are
sampled thread durations, including possible waits, not exact CPU attribution.
Full/themed diagnostic geometry hashes, element-type counts, root size and root
DPI match at mount and after 50 updates. Cache snapshots remain null throughout
the cold runs and stay initialized after the priming source is disposed.

The empty-source intervention is different from FW-004's attached-tree
prototype: it never attaches or renders the tree. Its creation/disposal cost is
included in mount time and allocations. It remains diagnostic, not a hidden
window policy inside ViewHost, a changed release default or release clearance.

Seven alternating untraced pairs completed through Test-Performance with
the same candidate payload on both sides, one side primed. They retain all four
standard workloads, 1,000 rows, 50 operations and existing budgets. The editor
case already creates a presentation target and is an intentional negative
control. This intervention mode is always ineligible for release.

| Workload | Mount time change | Update time change | Mount UI allocation | Update UI allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | -26.49% | -22.52% | -2.85% | -1.04% |
| Virtualized | -5.05% | -28.40% | -1.07% | -2.30% |
| Themed full list | -25.79% | -24.13% | -3.00% | -1.53% |
| Editors (already has target) | +1.31% | +1.89% | +0.04% | approximately 0% |

All 32 intervention checks stayed within their existing budgets; component work
matched. These compare initialization states of identical candidate payloads,
not two renderer source versions. One discarded warmup process per side,
seven measured processes, alternating order, SDK 10.0.402/runtime 10.0.13.

## Application fix and benchmark protocol

The reference sample now creates its **actual application window handle** with
WindowInteropHelper.EnsureHandle before constructing the large ViewHost tree.
The window remains application-owned; ViewHost gets no hidden window or new
public API, deferred lifecycle, DPI-policy override or GC-policy change.

Seven alternating actual-window startup pairs exercise the same sample binary,
50 unchanged sample rows, identical geometry and body work, native text editing
and complete disposal. They include window construction, host construction,
attachment, Show and layout (model creation excluded on both sides). Early-handle
startup measured 608.25 versus 758.41 ms (-19.80%), and 23,220,216 versus
23,816,232 UI-thread bytes (-2.50%). Every editing/lifetime check passed. This
is actual sample startup evidence, separate from the 1,000-row renderer cases.

The standard renderer harness now makes WPF target initialization explicit on
**both** source revisions, includes the small source's construction/disposal
inside mount metrics and retains the cold setup behind `-ColdPresentation`.
Protocol metadata is `process-dpi-initialization-v2`. The native/editor workloads
already own a target. This fixes the benchmark's accidental uninitialized
platform state; it does not advance the accepted revision, relax budgets,
remove controls/operations or erase any earlier failed result. The old attached-
tree prototype remains rejected: its extra composition cost is unnecessary.

Ordinary accepted-baseline verification passed all 32 gates, using the same
current initialized harness on both revisions and the unchanged accepted
`78c88fb901c202e3c2e49b6de300d1ce2369e00b` source and budgets. Its local
releaseEligible field is true; the intervention/startup-order campaigns remain
ineligible. An actual publication still requires committed-source CI and package
verification. No package was published or version advanced here.

| Against accepted source | Mount time | Update time | Mount UI allocation | Update UI allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +2.06% | -21.87% | -0.99% | -25.21% |
| Virtualized | -0.17% | -2.08% | -1.55% | -4.86% |
| Themed full list | -0.72% | -54.67% | -2.89% | -57.04% |
| Layout/editors | +2.22% | -5.29% | +0.13% | -3.29% |

Component work matched in every workload. These source-version comparisons
are separate from the same-binary cold/initialized speedups above. Mount time
increased slightly in two cases; within budget is not proof of zero regression.

## Profiling limits and failed diagnostics

Initial all-thread reports and large Monitor.Enter sampled-frame durations
must not be read as equivalent CPU or actual contention durations. Explicit
CLR contention events in two cold diagnostic runs recorded only about 1.1/2.3 ms
of total managed contention, not seconds. Real GC suspensions and sampler
`SuspendOther` events are separate; sample-profiler pauses are not GC pauses.

Two attempted paired profiles (`causal-primed`, `marked-primed`) accidentally
received the priming argument split into characters by PowerShell scalar
splatting. Their reports correctly say PresentationPrimed=false. They remain
preserved as **invalid primed comparisons**, never optimization evidence. The
seven-pair runner uses ProcessStartInfo.ArgumentList and verifies the flag in
every report, so its results are unaffected. `Collect-ComparisonTrace.ps1`
uses explicit argument lists and validates both the condition and enabled phase
provider before accepting a trace. Corrected final same-binary profiles were
collected after the ordinary timing campaign, without concurrent profiling.

The corrected profiles have 153 cold and 154 initialized phase intervals (the
extra interval measures initialization), verified identical Counter/core/WPF
assemblies, and equal geometry. The cold dispatcher interval recorded 4,092 ms
wall/3,719 ms UI CPU and 1,083 ms inclusive sampled DPI-fallback time. The
initialized interval recorded 3,375 ms wall/3,031 ms UI CPU and no samples in that
fallback. Cold mount-render sampled 1,401 ms in the fallback; initialized mount-
render sampled none. These establish an avoided path, not exact CPU costs from
sample percentages. Actual recorded dispatcher contention was 0.17 ms versus
0 ms, while GC suspension intervals overlapped approximately 762/784 ms.
CPU, GC overlap and inclusive sampled frames are not additive percentages.

The analyzer pairs runtime suspension starts/stops by runtime and initiating
thread: a sampler can restart while another thread begins GC suspension. Its
regression fixture checks this interleaving and phase clipping, avoiding false
attribution to GC or long managed lock waits. Explicit typed EventSource
boundaries and an enabled-provider check replace the initial unobserved dynamic
markers. Traced times never replace the untraced performance campaigns.

## Verification and preserved evidence

Full and themed geometry matched at mount/after updates in the initial checks;
virtualized geometry also matched after protocol adoption. The final isolated
probe verifies unchanged process awareness and **0 presentation sources before
and after** the empty source is disposed. The actual application retains its own
real window as before. No IME setting, DPI policy, GC policy or platform cache
is changed through private reflection.

Release validation passed: zero-warning build, 119 regression tests, 15 visual
checks, 21 stress assertions and actual-window startup/native-edit/disposal
integration. Both paired-analysis fixtures pass; the new suspension fixture is
included in CI. Final graph/documentation checks precede backlog completion.

[Archived evidence](performance-evidence/2026-10-24-dpi-bottleneck/) includes
raw repeated runs, binary/source manifests, exact measured source snapshots,
final tools, source for the pinned TraceEvent reader, phase/geometry data,
corrected and invalid profile runs, raw traces and installed-runtime inspection.
The exact installed runtime product version and DLL hashes are recorded; the
decompiled runtime inspection is read-only evidence, not a modified WPF runtime.

This identifies and avoids a demonstrated initialization bottleneck. It does
not claim a numerical attribution for every historical timing spike; those
failed runs remain failed. Tested evidence is from this Windows machine, SDK
10.0.402/runtime 10.0.13, SystemAware context and matching root DPI. Broader
physical-DPI/screen-reader/consumer acceptance remains separately tracked.

Implementation and local verification are complete; changes remain uncommitted.
Curated/generated graph and documentation checks passed. Local raw evidence:
`artifacts/fw003-reopened`, with preserved reproducible evidence archived above.
No new publication is implied; exact-source CI/package verification remains a
release requirement.
