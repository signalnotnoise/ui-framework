# FW-007 arbitrary reorder investigation â€” October 28, 2026

October 29 follow-up: the [mount-allocation investigation](fw007-mount-allocations-2026-10-29.md) resolves the remaining storage tradeoff and completes FW-007. This report preserves the earlier candidate and its evidence.

Follow-up: the [guarded move](fw007-guarded-move-2026-10-28.md) resolves the mixed-update timing blocker documented below. The large-list mount-storage tradeoff remains open. This report preserves the preceding candidate and its failed evidence.

Status: candidate under validation. This continues the [first safe optimization](fw007-keyed-shuffles-2026-10-28.md).
The before source is the preserved phase-one working tree, not a silently changed
accepted baseline. `artifacts/fw007-arbitrary/phase1-source` and its manifest
capture it before edits; raw candidates and results are retained separately.

## Cause and ownership

VirtualListControl previously used ObservableCollection: every IndexOf searched
linearly, and Move shifted an array. A 10,000-row attribution probe still took
about 183 ms for reverse and 134 ms for shuffle after detaching WPF listeners.
These single fresh-process timings identify storage as substantial work; they
are not additive phase measurements or release evidence.

The candidate keeps ordering in WPF, leaving core, editor lifecycle and native
container ownership unchanged. VirtualRowCollection implements read-only IList
and incremental notifications using an indexed AVL tree. Position lookup,
insertion, removal and move use subtree counts; row lookup uses a dictionary.
VirtualRowIndexNode owns balancing metadata only. VirtualRow records target and
removal indices during reconciliation to avoid secondary scans. No reset is used
to replace a reorder, no row objects are replaced, and the existing move ordering
and live-presenter guard remain intact.

An unchanged run walks successors in linear time rather than performing one
indexed tree lookup per row. Deletions record original indices and proceed in
descending order, after all removed readers are deactivated. Capacity reservation
avoids repeated dictionary growth during mount. IList mutation is renderer-owned;
WPF consumes its read-only view and notifications.

This bounds framework collection work; it does not promise a bound on WPF layout,
realization, native control work or the full-list panel's array operations.

## Evidence so far

The first indexed candidate passed all existing 148 tests. Three new permutation
cases initially failed because their move-only observer also watched the expected
Reset during final disposal. The observer now covers only the measured reorder
phase, retaining the assertion against Reset during reorder. All 151 tests pass,
including 60 repeated permutations at 1, 257 and 2,048 rows, index lookup,
enumeration, CopyTo, notification replay and 200 variable-size edits.

Seven-pair detached results for the first indexed candidate showed approximately
83% faster 10,000-row reverse and 72% faster shuffle, with identical move counts,
but slower rotations and filters. A traversal refinement restored rotation speed
and improved 10,000-row reverse/shuffle further; small filter cost prompted the
recorded-index and capacity refinement. These intermediates remain preserved.

Final validation is pending: repeated detached runs (including interleaving),
real-window virtualized and full-list editor workloads, and the unchanged
Test-Performance comparison. Reports include mount/update time and allocations,
component builds/mounts/unmounts and native editor counts. Full-list notifications
are unavailable and explicitly labelled; zero counters are not claimed as zero
native reorder work. The accepted baseline and budgets remain unchanged.

Physical IME/contrast acceptance remain FW-005/FW-006 work. This optimization
introduces no promise to preserve arbitrary native composition through recycling.

## Realized workload and lifetime follow-up

Seven-pair real-window diagnostics before the final link-cleanup correction
measured 10,000-row reversal at 420.2 -> 62.5 ms and shuffle at 248.8 -> 65.9 ms.
Both revisions built/mounted/unmounted 12 components and retained 12 native
editors. The matching 1,000-row full-list runs retained 1,000 editors with zero
component rebuilds or remounts. Its timings also varied despite unchanged full-list
code, so percentages are observations rather than blanket speedup guarantees.

A new weak-reference regression found that an invalidated iterator could retain
disposed rows through AVL links. The phase-one baseline passed that test; the
initial tree candidate failed. Removal and disposal now detach those links.
The final suite passes 153 tests, including stale-iterator cases with and without
an earlier row removal. Raw failure and baseline proof are retained. The full
renderer comparison and repeated final-source diagnostics are still pending.

## Mixed-update regression and sequential-read refinement

The first complete alpha.6 comparison exposed a 27.44% virtualized mixed-update
regression (855.7 -> 1090.5 ms), despite the large permutation gains. All raw runs
are retained in `artifacts/fw007-arbitrary/performance`; this result is not a pass.
Three diagnostic phase pairs localized most of the extra CPU time to the five
first-to-last moves: median dispatcher time 389.1 -> 615.2 ms. Sampled stacks
show WPF variable-height extent calculation reading consecutive collection
indices during notifications. A fresh logarithmic lookup for every index made
that consumer more expensive.

The collection now caches its last indexed node and walks a successor for the
next consecutive read. Every mutation invalidates this cursor before notifying
WPF. It does not change the move sequence, recycling, or composition safeguards.
All 153 tests pass after this refinement. Three diagnostic candidate runs reduced
mixed-update time to 860-879 ms; these probes are attribution evidence, not a
replacement for the repeated comparison. Final validation is running.

## Final cursor candidate validation (October 28)

The source hashes match the candidate captured by `cursor-performance`.
No accepted baseline or budget was advanced. The direct alpha.6 comparison
uses `62db456`, seven alternating measured process pairs plus one warmup per
side, SDK 10.0.402 and runtime 10.0.13 on this machine. This direct comparison
is not a public-release gate against the accepted baseline.

**Result: 31/32 broad comparison checks pass; FW-007 remains in progress.**
Virtualized mixed updates remain 14.61% slower (833.2172 -> 954.9188 ms),
exceeding the unchanged 10% timing budget. This supersedes the earlier
pending-validation statements above. The cursor reduced the first candidate's
regression, but did not eliminate it. All allocation/component-work checks pass.
Do not publish this candidate as regression-free.

| Broad workload | Mount time change | Update time change | Mount allocation change | Update allocation change |
| --- | ---: | ---: | ---: | ---: |
| full-list | +6.11% | +0.78% | +0.01% | +0.07% |
| virtualized | -13.09% | +14.61% | +0.73% | +0.02% |
| themed-full-list | +6.13% | +3.06% | +0.01% | +0.08% |
| layout-editors | -0.29% | -7.59% | -0.00% | -0.00% |

Component builds, mounts and unmounts match the baseline in all four workloads.
The virtualized workload performs 194 update builds, 98 mounts and 95 unmounts.

Supplemental runs compare the final source with the immutable phase-one FW-007
snapshot, not alpha.6. Each uses seven alternating pairs plus warmup; all retain
raw reports, mount/update allocations, component work and source snapshots.

| Mode | Rows | Operation | Baseline update ms | Candidate update ms | Change |
| --- | ---: | --- | ---: | ---: | ---: |
| refresh | 1,000 | filter | 1.394 | 1.547 | +10.92% |
| refresh | 1,000 | interleave | 2.747 | 2.535 | -7.70% |
| refresh | 1,000 | reverse | 3.918 | 3.033 | -22.60% |
| refresh | 1,000 | rotate | 1.050 | 1.175 | +11.86% |
| refresh | 1,000 | shuffle | 3.230 | 3.018 | -6.55% |
| refresh | 10,000 | filter | 18.706 | 16.512 | -11.73% |
| refresh | 10,000 | interleave | 90.195 | 27.270 | -69.77% |
| refresh | 10,000 | reverse | 187.158 | 29.601 | -84.18% |
| refresh | 10,000 | rotate | 15.147 | 12.947 | -14.52% |
| refresh | 10,000 | shuffle | 149.751 | 32.985 | -77.97% |
| layout | 1,000 | reverse | 29.967 | 28.465 | -5.01% |
| layout | 1,000 | shuffle | 35.830 | 27.059 | -24.48% |
| layout | 10,000 | reverse | 408.361 | 67.259 | -83.53% |
| layout | 10,000 | shuffle | 274.904 | 72.109 | -73.77% |

Tradeoffs are not hidden: detached 1,000-row rotation/filter cost approximately
0.13/0.15 ms more. At 10,000 realized-workload rows, mount allocation increases
from 25,892,936 to approximately 26,550,800 bytes (about 0.66 MB / 2.54%), while
update allocation is effectively unchanged. These supplemental allocations are
not covered by the standard 1,000-row gate. Both realized revisions retain 12
native editors and perform 12 builds/mounts/unmounts during each permutation.

The next bottleneck investigation should preserve the current composition-safe
move sequence while reducing WPF notification/index-read overhead. Also assess
the added per-row storage against large-list mount-allocation requirements.
Do not loosen the timing budget or promote the baseline to close this item.

The final full-list reference (1,000 editors, seven pairs) measured reverse
211.450 -> 195.487 ms and shuffle 207.738 -> 215.891 ms. Both retained 1,000
editors with zero update builds/mounts/unmounts. The full-list renderer was not
changed by the AVL work; these differences must not be attributed to it.

Final correctness validation passed: Release build (zero warnings/errors), all
153 tests, 15 visual checks, 21 stress assertions, documentation/action-pin checks
and the actual-window startup/editing/disposal check. This does not override the
failed performance comparison. Physical IME/contrast acceptance is unchanged.

[Archived evidence](performance-evidence/2026-10-28-indexed-shuffles/README.md)
includes final/intermediate raw runs, failed regression evidence, source snapshots,
traces and a SHA-256 manifest. No code was committed, published or baseline-promoted
as part of this validation. FW-007 remains in progress.
