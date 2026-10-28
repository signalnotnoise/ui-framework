# FW-006 evidence — October 26, 2026

Status: partial accessibility acceptance, not a release gate. See the
[implementation and acceptance record](../../accessibility-2026-10-26.md).

| Run | Code / purpose | Outcome |
| --- | --- | --- |
| [Peer-only predecessor](performance-predecessor/summary.json) | Before wrapper focus fix; seven measured pairs | 32/32 within budgets; does not verify final keyboard behavior |
| [Final runtime](performance-final/summary.json) | Row peers plus non-focusable/non-tab-stop lifetime wrappers; seven measured pairs | 30/32 pass; virtualized/editor mount +29.82% / +19.30% fail |
| [Identical binary](performance-identical-control/summary.json) | Hash-matched final payload on both sides; three measured pairs | 24/24 within budgets; virtual/editor mount shifts -13.83% / -9.98% despite equal code |
| [Focused repeat](performance-focused-repeat/summary.json) | Same final runtime against same predecessor; seven measured pairs with full-list retained | 24/24 within budgets; full/virtual/editor mount +0.96% / +3.33% / -21.26% |

The failed run remains failed. The control and repeat show substantial timing
variation; they neither establish a causal speedup nor prove the absence of a
regression. All final-run allocation/component-work checks pass. Mount timing
repeatability remains open. Accepted baseline `78c88fb` and budgets are unchanged.
These are custom-predecessor/control/focused comparisons, not release acceptance.

Raw per-process reports, CPU/wall timings, stdout logs, paired statistics and
source/binary manifests remain in each run directory. One warmup per side is
discarded; measured order alternates. Workload: 1,000 rows, 50 mixed operations,
SDK 10.0.402, runtime 10.0.13, process-dpi-initialization-v2.

`baseline-runtime-source.zip` preserves the local measurement snapshot
`fe0efac8649b5d9c9565e1404b251516551d5a5f`, including the uncommitted FW-005
runtime and the same Counter harness. It is not a branch update. Each comparison
preserves its own candidate source ZIP. `archive-manifest.json` records SHA256
and byte counts; preservation verified current final runtime sources and three
measured DLL hashes, and checked both identical-control payload maps for equality.

Correctness evidence: 139 tests, 15 visual checks, 21 stress assertions and the
actual-window startup/editing/disposal check pass. The isolated predecessor
produces three expected failures in the five new accessibility checks; the final
five pass. Earlier provider/popup/focus harness failures are also retained.
Verification-source ZIPs preserve the final tests and manual review window.
The manual-accessibility JSONL files are explicitly smoke tests, including one
failed harness setup followed by a pass. No Narrator, system contrast-theme or
physical scaling acceptance is claimed.
