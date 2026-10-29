# FW-007 mount allocation investigation — October 29, 2026

Status: implementation and validation complete locally. FW-007 is complete; no package is published by this work.

## History and attribution

Git history was searched before pursuing the first-update slowdown further.
`be6e1d1` (FW-003) established the need to measure platform initialization and
separate dispatcher/layout/GC work. `fd72534` saved theme mount allocations by
batching resource invalidations but did not equate that saving with lower latency.
`2db7a9a` reduced redundant native detachments. The preceding uncommitted FW-007
reports separately identified indexed-read costs in WPF extent processing.
No committed version of the new collection-batching experiment was found.

Initial storage changes replaced separate AVL nodes and a row-to-node dictionary
with links on VirtualRow, recovered about 524 KB at 10,000 rows, and preserved
rank lookup, successor traversal and incremental notifications. Removing the
initial permutation buffer left about 54 KB above the original phase-one mount.

A rejected experiment published initial rows with one Reset. It reduced 10,000-row
mount allocations from 25.89 MB to 16.19 MB, but first updates became roughly 8 ms
slower than the guarded source. Reusing the normal AVL insertion path did not fix
that penalty. Separate phase diagnostics measured Gen 1 collections during the
candidate update, versus only Gen 0 in the baseline, with about 13 versus 6 ms of
process-wide GC pause time. Refresh dominated; post-refresh layout remained about
2 ms. This accounts for most of the difference, without establishing every source
of latency. No forced collection, GC policy, workload reduction or budget change
was introduced. The experiment and its raw results are retained, not accepted.

## Retained implementation

VirtualRowCollection owns balancing and membership; VirtualRow holds its intrusive
links. Zero height marks detached rows; root identity rejects foreign collections.
Removal/disposal sever links so retained iterators cannot hold the rest of a tree.
Retained and removed rows reuse a position field because those roles do not overlap.
An initially empty list validates descriptions first and inserts in their order
without allocating a permutation array. Individual Add notifications remain.

The two optional snapshot references share a VirtualRowSavedState holder, allocated
only when component or focus state is present and released when both clear.
Unvisited logical rows pay for one reference rather than two. This does add a small
holder allocation for saved rows; measurements must include update/recycling costs,
not only initial mount. Presenter composition, capture, restoration and ownership
remain unchanged. This is not a bound on retained memory after visiting every row.

Correctness includes foreign/removed/disposed membership, repeated permutations,
variable cardinality, iterator retention, focus, selection, stale bindings and
composition guards. The batch-only test was retained with the rejected prototype;
it is not a guarantee of the final implementation.

## Validation protocol

Final comparisons use seven alternating measured process pairs and one warmup per
side, SDK 10.0.402/runtime 10.0.13 on the same machine. Large-list comparisons retain
the original phase-one snapshot and the preceding guarded snapshot as distinct
baselines. The broad unchanged Test-Performance harness includes full-list,
virtualized, themed-full-list and layout-editors against alpha.6 (`62db456`).
That direct comparison is not an accepted-baseline public-release gate.
The accepted revision remains `78c88fb901c202e3c2e49b6de300d1ce2369e00b`.

Final results below retain the initial failed comparison and the focused confirmation.

## Completed large-list comparisons

### Real-window final candidate versus the preceding guarded source

| Rows | Operation | Update ms before / after | Change | Mount bytes before / after | Update bytes before / after |
| ---: | --- | ---: | ---: | ---: | ---: |
| 10,000 | reverse | 64.469 / 63.531 | -1.46% | 26,550,832 / 25,870,616 | 19,160,368 / 19,160,752 |
| 10,000 | rotate | 18.778 / 18.736 | -0.22% | 26,550,832 / 25,870,616 | 12,118,608 / 12,118,640 |
| 10,000 | shuffle | 66.712 / 67.481 | +1.15% | 26,550,832 / 25,870,616 | 19,122,784 / 19,123,168 |

### Real-window final candidate versus phase one

| Rows | Operation | Update ms before / after | Change | Mount bytes before / after | Update bytes before / after |
| ---: | --- | ---: | ---: | ---: | ---: |
| 1,000 | reverse | 27.427 / 26.412 | -3.70% | 5,562,856 / 5,569,960 | 3,188,192 / 3,188,264 |
| 1,000 | rotate | 25.792 / 7.253 | -71.88% | 5,562,856 / 5,569,960 | 3,165,680 / 1,330,464 |
| 1,000 | shuffle | 28.233 / 27.137 | -3.88% | 5,562,856 / 5,569,960 | 3,185,888 / 3,151,832 |
| 10,000 | reverse | 402.007 / 63.660 | -84.16% | 25,893,032 / 25,870,616 | 19,160,680 / 19,160,752 |
| 10,000 | rotate | 201.399 / 18.448 | -90.84% | 25,893,032 / 25,870,616 | 19,137,856 / 12,118,640 |
| 10,000 | shuffle | 246.116 / 68.161 | -72.31% | 25,893,032 / 25,870,616 | 19,144,312 / 19,123,168 |

### Detached descriptions versus phase one

| Rows | Operation | Update ms before / after | Change | Mount bytes before / after | Update bytes before / after |
| ---: | --- | ---: | ---: | ---: | ---: |
| 10,000 | filter | 19.747 / 17.449 | -11.63% | 19,469,816 / 19,447,000 | 7,824,760 / 7,849,048 |
| 10,000 | interleave | 86.721 / 26.303 | -69.67% | 19,469,816 / 19,447,000 | 11,530,848 / 11,530,536 |
| 10,000 | reverse | 166.232 / 27.641 | -83.37% | 19,469,816 / 19,447,000 | 13,017,696 / 13,017,384 |
| 10,000 | rotate | 15.139 / 12.523 | -17.28% | 19,469,816 / 19,447,000 | 7,738,752 / 7,738,440 |
| 10,000 | shuffle | 142.429 / 31.768 | -77.70% | 19,469,816 / 19,447,000 | 13,012,416 / 13,012,104 |

The guard comparison retains 12 native editors. Rotation builds/mounts/unmounts
one component on both sides; reversal/shuffle build/mount/unmount 12 on both sides.
Against phase one, rotation reduces that work from 12 to one; reversal/shuffle
remain identical. Detached descriptions have no native editors or component work.
Raw reports retain mount timing as well as all counters. For example, versus the
guard, 10,000-row mount medians are 1022.20→1005.16 ms for rotation,
986.22→988.82 ms for reversal, and 997.42→952.20 ms for shuffle. These small,
direction-dependent timing differences are not a blanket mount speedup claim.

The final 10,000-row real-window mount saves 680,216 bytes (2.56%) versus the
guarded indexed candidate and 22,416 bytes (0.087%) versus phase one, eliminating
the original additional mount allocation. At 1,000 rows, mount allocations remain
7,104 bytes (0.13%) above phase one. Saved-state holders add 32 bytes to the
rotation update and 384 bytes to reverse/shuffle versus the guard. Detached
filtering adds 24,288 bytes (0.31%) versus phase one. These small increases are
reported; the workload and budgets are unchanged.

## Broad comparison and editor mount failure

The first unchanged alpha.6 comparison passed 31/32 checks. Editor mount time
failed at +14.74% against the 10% budget (2122.84→2435.72 ms). The failed campaign
is retained. The editor workload creates Scroll/AdaptiveGrid/TextEditor, not a
VirtualList; its renderer/layout/style sources are unchanged from alpha.6.
Individual mount pairs range from -38.15% to +43.21%. That variability warrants
an identical-binary control and a focused source comparison, not erasing the failure.

| Workload | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| full-list | -4.86% | +5.59% | -0.03% | -0.06% |
| virtualized | +4.71% | -51.04% | +0.07% | -20.09% |
| themed-full-list | -12.34% | -1.14% | +0.10% | +0.03% |
| layout-editors | +14.74% | -2.98% | -0.00% | -0.75% |

Full-list and editor component work matches alpha.6. The virtualized workload
performs 165 versus 194 update builds, 69 versus 98 mounts and 66 versus 95
unmounts, retaining the guarded-move gain for the same 50 operations. Mount builds
remain 27. Themed component work also matches. All allocation checks pass.

## Final verification and limits

The seven-pair identical-binary editor control passed 8/8 checks but measured
+6.39% mount and +3.77% update time between identical payloads. One individual
mount pair differed by 19%. The separate unchanged-source confirmation also
passed 8/8: editor mount 2518.24→2165.03 ms (-14.03%), update 8486.49→8426.99 ms
(-0.70%), effectively identical allocations and identical component work.
Do not call that an editor speedup: the earlier +14.74% failure remains preserved,
and the source path is unchanged. Current evidence is the broad 31/32 campaign
plus this focused passing confirmation, not a retroactively passing broad run.
No budget was loosened, failed sample discarded, or accepted baseline promoted.

Test-Release passed with zero build warnings/errors, all 159 framework tests,
15 visual checks, 21 stress assertions, documentation/action-pin checks and
actual-window startup/editing/disposal. Source manifests for all three broad/control
campaigns match the final working files; the three final supplemental runtime
snapshots also match. These checks support closing FW-007's large-keyed-list work,
not a claim of zero timing variation or a public-release gate. Physical IME and
contrast acceptance remain FW-005/FW-006.

[Archived raw evidence and source snapshots](performance-evidence/2026-10-29-mount-allocations/README.md)
include the rejected allocation/batching variants, phase/GC diagnostics, final
comparisons, correctness results, source audit and SHA-256 manifest. Both knowledge
graphs are refreshed from the final source and validated separately.
