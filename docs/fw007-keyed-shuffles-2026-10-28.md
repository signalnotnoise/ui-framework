# FW-007 keyed collection investigation — October 28, 2026

Status: in progress. Baseline source is alpha.6 (`62db456`, documentation follow-up
to runtime commit `fd72534`). Accepted release baseline and budgets are unchanged.

The curated and extracted graph neighborhoods identify WPF VirtualListControl as
owner of logical row ordering, VirtualRowPresenter as owner of realization/editing
lifetime, and ViewHost as owner of full-list panel reconciliation. Core APIs need
no changes for the first candidate.

Initial detached diagnostics at 1,000/5,000/10,000 logical rows show that a single
first-to-last move emits N-1 Move notifications. At 10,000 rows the first probe
measured 104 ms and 9,999 moves; reverse and random shuffle measured 173/137 ms.
These single-process observations exclude realization/layout and are not release
performance evidence. Row objects and final order were verified in each probe.

The first candidate reduced a logical forward move to one notification, but failed
an existing active-composition test: WPF detached the moved live presenter before
composition completion. That candidate is rejected; the assertion is retained.
The narrowed candidate applies the shortcut only to an unrealized displaced row.
Realized and focus-pinned rows retain the existing path. New keys insert directly
without scanning the retained collection. ItemsSource and retained row objects
remain stable; no Reset is introduced.

New tests cover an offscreen forward move with one notification, retained visible
input/selection/binding, and 200 deterministic mixed insert/remove/reorder updates.
The narrowed initial candidate passed 148 framework tests. Final validation below covers the refined candidate. FW-007 remains open for
general shuffles and realized-row movement.

`tools/Test-KeyedShuffles.ps1` runs the identical diagnostic against archived
baseline and candidate sources, alternates order, excludes a warm-up pair from
analysis and retains seven measured pairs. It supplements, not replaces,
`tools/Test-Performance.ps1` with full-list, virtualized, themed and editor workloads.

Remaining: arbitrary random/reverse ordering still uses linear searches and
array-backed Move operations; WPF realized-row moves also require an explicit
safe editing policy. FW-007 remains in progress until that larger scope is
measured and resolved or bounded with an explicit contract.

## Refined detached diagnostic

Seven measured alternating pairs after a warm-up pair use exactly the same
harness against alpha.6 and the refined candidate. The refinement skips the
new-key dictionary check entirely on updates containing only retained rows.

| Rows | Workload | Baseline ms | Candidate ms | Time change | Allocation change |
| ---: | --- | ---: | ---: | ---: | ---: |
| 1,000 | Forward rotation | 3.483 | 1.390 | -60.10% | -41.76% |
| 1,000 | Reverse | 3.852 | 3.855 | +0.09% | +0.01% |
| 1,000 | Random shuffle | 3.527 | 3.713 | +5.28% | -0.12% |
| 1,000 | Filter half | 1.614 | 1.512 | -6.33% | unchanged |
| 10,000 | Forward rotation | 108.672 | 16.589 | -84.73% | -41.57% |
| 10,000 | Reverse | 169.782 | 174.052 | +2.52% | -1.47% |
| 10,000 | Random shuffle | 146.688 | 139.361 | -4.99% | -0.02% |
| 10,000 | Filter half | 19.344 | 19.237 | -0.55% | unchanged |

Forward rotation falls from N-1 to one Move notification. Random and reverse
results are mixed, with cold-process timing/allocation variation; these do not
establish a general improvement or a new complexity bound. The first paired
candidate's 10,000-row random median regressed 7.44%; its evidence is retained.
Full renderer comparison against alpha.6 passed all 32 checks. Final correctness:
148/148 tests pass. No release or baseline change is authorized by this diagnostic.

## Final renderer and correctness validation

`Test-Performance.ps1 -BaselineRef 62db456 -IncludeLayoutEditors` completed seven
alternating measured pairs plus warm-up, with the same harness/SDK/machine on
both sides. This is a direct alpha.6 comparison; the accepted release baseline
`78c88fb` and all budgets remain unchanged. All 32 checks passed.

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | -0.46% | -3.05% | +0.01% | +0.08% |
| Virtualized | -21.71% | +2.06% | -0.01% | -0.01% |
| Themed full list | +1.20% | +4.47% | -0.01% | +0.01% |
| Layout/editors | +2.82% | +0.22% | approximately unchanged | approximately unchanged |

Component builds, mounts and unmounts match in every workload. Timing changes
in untouched workloads show why these medians are observations, not proof that
all latency changes come from this optimization. Positive changes are retained
and within existing budgets. The strong detached rotation result does not imply
an equivalent full-application improvement or faster arbitrary shuffles.

Final release validation: 148/148 tests, 15 visual checks, 21 stress assertions,
actual-window startup/edit/disposal, documentation and curated graph checks pass.
The mixed regression test varies collection size over 200 deterministic updates.
No active-editor assertion was weakened. The failed first prototype, first paired
results, refined results and interrupted preliminary renderer run are retained.

[Raw runs, source snapshots, tests and SHA-256 manifest](performance-evidence/2026-10-28-keyed-shuffles/manifest.json).

Next work remains the arbitrary-shuffle bottleneck: repeated IndexOf scans and
array-backed shifts, plus WPF's detach behavior for live presenters. Measure a
replacement ordering strategy against reversal, random permutations and filtering
with realized rows and active editing before broadening the shortcut. Do not use
collection Reset to hide that work or sacrifice retained state. FW-007 is **in
progress**, not complete. Changes are local and unpublished.
