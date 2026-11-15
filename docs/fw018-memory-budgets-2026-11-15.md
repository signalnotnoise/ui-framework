# FW-018 scoped memory budgets — November 15, 2026

## Decision and scope

The new [memory acceptance contract](memory-budgets.md) separates owned-resource
cleanup from whole-process budgets. It checks both: balanced ownership and zero
tracked survivors are mandatory even when memory stays below a ceiling.

The numerical [policy](../tools/memory-budgets.json) was frozen before the fresh
campaign. It budgets post-cleanup peaks, first-to-final median growth and
midpoint-to-final median growth independently for each process. No inferred WPF
cache allocation is subtracted. Browser child-process count/private bytes also
have ceilings; working sets are informational because shared pages prevent an
exclusive sum.

Native coverage is three fresh processes at each of 500 and 5,000 logical rows,
each with three warmup and 180 measured batches of four workspace cycles. Browser
coverage is three fresh-profile and three reused-profile processes, each with
four warmup and 128 measured local parent/popup cycles. This does not promise
budgets for arbitrary sites, application data or external accessibility clients.

## Fresh validation

All **12/12 processes pass**, with no evidence errors. The six native runs cover
1,098 sampled batches / 4,392 workspace cycles including warmup (4,320 measured
cycles). The six browser runs cover 792 parent/popup cycles including warmup
(768 measured). Raw samples match reconstructed reports; bounded reporting,
negative controls, ownership and shutdown checks pass.

Worst observations across native runs: collected heap 1,853,296 bytes, total-window
growth 5,400 bytes and late growth zero; private bytes peak 312,385,536 and late
growth 655,360; handles peak 460 with no positive late growth. GDI peaks at 20 and
USER at 17, with neither growing across the median windows.

Worst browser-host observations: heap 1,263,992 bytes, total-window growth 91,712
and late growth 1,208; private bytes peak 290,893,824, total growth 7,081,984 and
late growth 9,209,856. Handles peak at 551, with total/late growth at most 16.
GDI peaks at 18 with zero window growth; USER peaks at 18 with total growth one
and late growth zero. Live captured browser groups peak at five processes and
170,930,176 summed private bytes. These maxima may occur in different processes.

The first fresh-profile run reaches the +16 late-handle limit exactly. It passes
but has no headroom on that metric; preserve this as a watch point. The earlier
+20 outlier still fails the same limit and is not explained by these fresh passes.

Environment: Windows 11 build 26200, SDK 10.0.402, .NET 10.0.13 and WebView2 runtime
157.0.4341.46. Framework source hashes and runtimes agree across campaigns.
The frozen policy/result retain the pre-run candidate status; the repository
policy changes only status to `validated-local-workloads` after evaluation.

[Archived evidence](verification-evidence/2026-11-15-memory-budgets/README.md)
contains raw samples, reports, source manifests, source snapshots, frozen policy,
calibration failures, fixture output and per-entry hashes. Local original:
`artifacts/fw018-memory-budgets-2026-11-15`.

## Calibration and limitations

Preserved earlier bounded-reporting evidence is calibration, not a substitute for
this campaign. Applying the new policy still flags the older native GUI/input
step and the older fresh-profile browser's +20 late-handle step. Those failures
remain in `historical-calibration.json`; the browser step's object identities have
not been established. A passing fresh campaign would not explain that outlier.

The native harness rejects observed input or missing activation evidence for the
quiet profile. The browser harness does not observe native-HWND input. GuidedGrade
was absent at a recorded process observation; this was not a continuous audit.
Live browser snapshots can miss transient peaks between observations. Finite-run
budgets cannot establish universal leak freedom.

## Implementation and remaining work

`Test-MemoryBudgets.ps1` creates a fresh evidence directory and copies the policy
before invoking the existing measurement workloads. Its evaluator validates raw
sample equivalence, workload identity, bounded reporting, source/runtime agreement,
negative retention controls, cleanup and shutdown before reporting budget results.
Missing evidence and any failing process make the command fail. Sensitivity
fixtures cover ongoing drift, isolated peaks, invalid/missing data and browser
ownership/process failures despite small heaps. CI runs these fixtures; the full
interactive campaign remains a local acceptance command.

No core or WPF runtime source changed during this budget task. Hash comparison
with the preceding peer-cache performance candidate covers all 58 framework
source-manifest entries. Its 176 passing tests and 32/32 local performance checks
are prior evidence, not fresh runs in this task. The existing timing/allocation
baseline and budgets are unchanged; no hosted release-gate result is claimed.

The user split the remaining scope on November 15: contextual failure diagnostics
are FW-025 and physical Narrator revalidation after peer-cache cleanup is FW-026.
Both are post-v1 follow-ups. FW-018 is complete within the agreed lifetime/memory
scope; that does not claim those deferred checks were performed.
