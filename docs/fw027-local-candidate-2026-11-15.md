# FW-027 local candidate — November 15, 2026

Candidate: `0.1.0-alpha.8-local.1`, built from the current uncommitted source.
Both library and symbol packages are in `artifacts/packages-alpha.8-local.1`.
Package hashes and a complete framework source snapshot identify this dirty-tree
build; the embedded repository commit alone cannot identify uncommitted changes.
Nothing has been published, committed or pushed by this task.

## Validation

Full Release validation passes: 176 framework tests, reporting-retention controls,
15 visual stress checks, 21 stress assertions, real-window startup/edit/disposal,
native ownership/shutdown and the short memory/reporting check. The short run has
zero tracked survivors and zero owned native bytes. Both public API analyzer guards
reject deliberately invalid API baselines, then the restored baseline builds.

Fresh installation from the local packages passes transitive dependency, state,
retained rendering, themed rich content, native interop/style isolation, compact
input/button and cleanup checks. Native interop's single smoke timing is diagnostic,
not a performance comparison.

Memory-budget, native-memory and attribution fixtures pass; all 25 graph-tooling
fixtures pass. The existing 12-process scoped sustained-memory campaign matches
all 54 core/WPF source entries in its manifest. That evidence is reused with explicit
hash provenance; it was not rerun or described as a fresh long soak in this task.

## Completed comparisons

The full source comparison uses alpha.7's published source commit
`c5d639bcff707ff1a75b23f2a60135d6be6cb7b2`, the current identical benchmark workload
on each side, SDK 10.0.402 / runtime 10.0.13, seven alternating measured pairs plus
warmups, and full-list, virtualized, themed-full-list and editor scenarios.
All **32/32 comparisons pass** without changing workloads or thresholds.
Mount/update timing changes are −1.42%/−0.79% full-list, −2.30%/−0.87%
virtualized, −0.36%/−0.02% themed, and −0.56%/−1.71% editors. UI allocation
changes are approximately −0.04% to +0.001%; component builds/mounts/unmounts
match. Small timing improvements are observations, not a broad speedup claim.
All 58 packaged framework manifest entries match the benchmark source snapshot.
The comparison's `releaseEligible` is false because alpha.7 is the requested
reference rather than the unchanged accepted release baseline.

The standalone package comparison uses public alpha.7 and the exact local candidate
packages in separate fresh caches. All **14 processes pass** across full-list and
virtualized modes: three alternating pairs per mode plus an unchanged alpha.7
consumer binary using the candidate DLLs. All 56 workspace lifetimes balance
native/component ownership; all 168 tracked host/editor probes collect, with
cleanup and dispatcher shutdown complete. Installed `.nupkg` hashes match the
local feed, and loaded DLL hashes match the selected package binaries.

This smaller diagnostic workload has full-list mount/update changes +7.40%/−14.87%
and virtualized −0.30%/+4.01%. Full-list mount/update allocations change
−0.047%/−1.399%; virtualized allocations and all update build counts are effectively
unchanged. The increases are retained and within the existing 10% timing reference
threshold; this fixture itself is not the release gate.

Inspection of the raw process medians shows substantial mount variation:
full-list baseline 137.509, 62.766, 69.933 ms versus candidate 75.105, 73.851,
75.807 ms. Virtualized update ranges overlap: baseline 60.975–65.016 ms versus
candidate 60.775–66.168 ms. The larger seven-pair benchmark does not reproduce
those increases. This does not establish their cause or prove an absence of small
regressions; no speculative runtime change, baseline advance or rerun-to-green was
made. Candidate post-shutdown heap is 1,536,728–1,554,520 bytes, private bytes
206,671,872–208,474,112, handles 401–402; these are observations, not new budgets.

The adoption driver's optional local-feed mode is reusable. Without that option,
both versions must remain publicly indexed, as before.

## Release boundary

This requested alpha.7 comparison is not the accepted release-baseline gate.
`tools/performance-baseline.json` still points to `78c88fb` with unchanged budgets.
A future release needs a committed exact candidate and successful hosted validation
against that accepted baseline, plus package acceptance. Passing a dirty-tree local
comparison does not establish a hosted pass or authorize publication.


## Evidence and outcome

FW-027 completes the requested local packaging/validation work. The four package
files and their SHA-256 manifest remain in the local artifact directory. Full logs,
TRX/results, seven-pair raw runs, package-upgrade results, source snapshots and
package hashes are [archived](verification-evidence/2026-11-15-local-alpha8-candidate/README.md).
The source/package comparison passed; exact committed-candidate hosted validation
and any publication remain separate next steps. No claim of a new physical
IME/Narrator/contrast campaign is made.
