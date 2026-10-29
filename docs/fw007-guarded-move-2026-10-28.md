# FW-007 guarded forward move — October 28, 2026

October 29 follow-up: the [mount-allocation investigation](fw007-mount-allocations-2026-10-29.md) resolves the remaining storage tradeoff and completes FW-007. This report preserves the earlier candidate and its evidence.

Status: guarded-move implementation and validation complete. All 158 tests and
performance checks pass within existing budgets. FW-007 remains open for the
preceding indexed collection's mount-storage tradeoff. This is local work, not a
package release.

## Ownership and guard

The preceding indexed candidate reduced large permutations but failed the broad
mixed-update timing comparison by 14.61%. Phase traces localized most extra work
to first-to-last moves and WPF notification/extent processing. The earlier safe
path moved intervening rows when the displaced row had a presenter.

VirtualListControl can now move the displaced row once when its successor is the
next desired row, its destination is forward and unrealized, and the presenter
reports no pending interaction. VirtualRowPresenter owns that interaction check:
keyboard focus, mouse capture, pending focus restoration, own/nested composition,
composition completion and recorded composition failure all block the shortcut.
The visual-tree inspection is read-only and does not pump the dispatcher or finish
composition. Existing release/snapshot ownership remains unchanged. Offscreen
rows retain the existing shortcut; realized destinations retain the conservative
path. ItemsSource, row objects and incremental notification semantics are retained.

The guard covers WPF interaction tracked by the presenter; it does not add a
physical IME acceptance claim or take ownership of native-island input providers.

## Regression evidence

Five real-window cases cover inactive offscreen movement, focused editing,
composition without keyboard focus, nested composition without focus, and a
realized destination. The inactive path emits one Move, preserves the neighboring
editor and its selection/binding, ignores late edits to the detached editor,
and restores the moved row with the correct binding on return. Other cases
require multiple conservative moves; composition completion must occur while
the original editor still has a presentation source. Focused editing follows
the moved row and still updates its own state.

The first test draft incorrectly required neighbor control identity on the
pre-existing conservative path. Running it against the preceding source showed
the same failures. Corrected tests assert that identity for the new inactive
shortcut and verify the existing active-path binding/attachment guarantees.
The final before-guard source passes four guard cases and fails only the new
single-move requirement; the candidate passes all five and all 158 framework tests.
No pre-existing composition assertions were weakened. Raw drafts and baseline
proof are retained with final validation evidence.

## Performance protocol

The unchanged broad harness compares alpha.6 (`62db456`) with this candidate,
including full-list, virtualized, themed controls and layout/editors. Supplemental
real-window rotation, reversal and shuffle compare the immediately preceding
indexed/cursor candidate (archived source from the previous validation) with
this guard, including the 1,000-editor full-list reference. Each comparison uses
seven alternating measured process pairs plus a warmup per side, the same harness,
SDK 10.0.402, runtime 10.0.13 and machine. Raw results include mount/update time,
UI-thread allocations, component work and native editor counts. Budgets and the
accepted baseline remain unchanged. Final results are recorded below.

## Completed correctness and broad comparison

Release validation passed: zero build warnings/errors, all 158 tests, 15 visual
checks, 21 stress assertions, documentation/action-pin checks and actual-window
startup/editing/disposal. The final test cleanup closes its window even if a
composition assertion fails. Existing composition tests remain unchanged.

The broad comparison reports 32/32 checks within the unchanged budgets. Its
virtualized workload improves 55.05% in update time and 20.07% in UI-thread
update allocations. Component builds fall from 194 to 165, mounts from 98 to 69,
and unmounts from 95 to 66 for the same 50 mixed operations.

| Workload | Mount time change | Update time change | Mount allocation change | Update allocation change |
| --- | ---: | ---: | ---: | ---: |
| full-list | +8.33% | +3.15% | +0.02% | -0.06% |
| virtualized | -2.11% | -55.05% | +0.73% | -20.07% |
| themed-full-list | -12.52% | +2.94% | +0.27% | +0.00% |
| layout-editors | -2.06% | +1.22% | +0.00% | -0.91% |

The themed baseline's sample 1 was interrupted: process wall time 552.65 seconds
versus 43.56 seconds CPU, including a 538.57-second mount measurement. The raw
sample is retained and was not silently removed from the median calculation.
The virtualized pairs finished before this interruption. A separate fresh seven-
pair themed comparison passed all eight checks; all 16 process runs completed
within 16.77 seconds. No sample was removed or budget changed.

## Guard-isolating real-window comparisons

These compare the preceding indexed/cursor source with the guard, not alpha.6.
All values are medians of seven alternating measured pairs after warmup.

| Mode | Rows | Operation | Baseline update ms | Candidate update ms | Change | Baseline / candidate update bytes |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| layout | 1,000 | reverse | 24.162 | 25.256 | +4.53% | 3,187,880 / 3,187,880 |
| layout | 1,000 | rotate | 25.358 | 7.100 | -72.00% | 3,165,368 / 1,330,432 |
| layout | 1,000 | shuffle | 25.206 | 26.173 | +3.84% | 3,185,576 / 3,151,448 |
| layout | 10,000 | reverse | 61.729 | 62.927 | +1.94% | 19,160,368 / 19,160,368 |
| layout | 10,000 | rotate | 62.099 | 18.711 | -69.87% | 19,137,544 / 12,118,608 |
| layout | 10,000 | shuffle | 65.450 | 66.093 | +0.98% | 19,144,000 / 19,122,784 |
| full-layout | 1,000 | reverse | 219.230 | 210.546 | -3.96% | 11,889,392 / 11,889,392 |
| full-layout | 1,000 | shuffle | 199.516 | 209.420 | +4.96% | 11,801,520 / 11,801,520 |

Both virtualized revisions retain 12 native editors. Rotation builds/mounts/
unmounts fall from 12 each to 1 each; reversal/shuffle remain at 12 each. The
full-list reference retains 1,000 editors and zero update builds/mounts/unmounts.
Full-list timing variation is not attributed to this guard, which it does not use.

The guard adds only 80 measured mount bytes in both the 1,000- and 10,000-row
virtualized diagnostics (no additional per-row storage). The preceding indexed
collection's approximately 0.66 MB / 2.54% mount-allocation increase at 10,000 rows
versus phase one is not fixed by this guard. FW-007 remains open for that storage
tradeoff. Inspect per-row node/index duplication and preserve the accepted budgets;
there is no baseline promotion, release or claim of zero cost in every workload.

## Clean themed confirmation and final evidence

The fresh `themed-uninterrupted` run passes all eight checks: mount time +7.98%,
update time +0.44%, mount allocations -0.14%, update allocations -0.18%, and
unchanged component work. These are within the existing 10% time / 2% allocation
budgets. Both broad and focused source manifests exactly match the final runtime
source. Timings vary between runs; use paired results rather than comparing
absolute medians across different sessions.

[Archived evidence](performance-evidence/2026-10-28-guarded-move/README.md)
contains 696 hash-verified raw/source entries, including initial test failures,
pre-guard proof, final tests, all repeated runs, the interrupted themed sample
and its clean confirmation. The earlier failed candidate remains in its separate
archive. The accepted baseline and budgets are unchanged. Nothing was committed
or published. The mixed-update timing blocker is resolved; FW-007 remains open
only for the previously identified large-list mount-storage follow-up in scope.
