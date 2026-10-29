# FW-007 guarded single-move evidence — October 28, 2026

Guard implementation and validation are complete. The broad alpha.6 comparison
passes 32/32 checks; a clean themed repeat passes 8/8. All 158 tests, 15 visual
checks, 21 stress assertions and actual-window checks pass. Budgets are unchanged.

See the [report](../../fw007-guarded-move-2026-10-28.md) for workload tables,
measurement boundaries, initial test corrections and the remaining storage cost.

- `raw-results.zip`: all raw repeated runs, final correctness evidence, initial
  test failures, pre-guard proof, source/binary manifests and the clean themed repeat.
- `candidate-source.zip`: final framework, tests, samples and harness sources.
- `manifest.json`: SHA-256 hashes for all 696 archived entries, verified after writing.

`performance` compares alpha.6 (`62db456`) with the guard. It retains the themed
sample interrupted for 553 seconds; `themed-uninterrupted` is the clean seven-pair
confirmation (all 16 processes complete within 16.77 seconds). No samples were
silently removed. `layout` and `full-layout` compare with the previous indexed/
cursor source archived at [the prior evidence directory](../2026-10-28-indexed-shuffles/README.md).

The guard reduces mixed-update duration 55.05% and update allocations 20.07%. FW-007 stays
open for the preceding indexed collection's roughly 0.66 MB / 2.54% additional
mount allocations at 10,000 rows. This local validation does not publish packages
or advance the accepted baseline. Physical IME/contrast acceptance is separate.
