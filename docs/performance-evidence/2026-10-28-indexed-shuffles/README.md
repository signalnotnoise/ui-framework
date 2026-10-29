# FW-007 indexed shuffle validation — October 28, 2026

**Not accepted for release:** 31/32 broad comparison checks pass. Virtualized
mixed updates are 14.61% slower than alpha.6, exceeding the unchanged 10% budget.
All 153 tests, 15 visual checks, 21 stress assertions and actual-window checks pass.

See the [analysis and workload tables](../../fw007-indexed-shuffles-2026-10-28.md).

- `raw-results.zip`: final and intermediate reports, failed tests, phase probes,
  sampled traces, raw repeated runs and final release correctness evidence.
- `candidate-source.zip`: final framework, sample, tests and benchmark sources.
- `phase1-source.zip`: immutable supplemental comparison baseline (earlier FW-007).
- `manifest.json`: SHA-256 hashes for every archived entry.

In raw results, `cursor-performance/summary.json` is the final broad comparison
against alpha.6 commit `62db456`. `cursor-paired`, `cursor-layout` and
`cursor-full-layout` are seven-pair final-source diagnostics against the phase-one
snapshot. `performance/summary.json` retains the earlier 27.44% regression.
Do not confuse intermediate reports with final-source evidence. Diagnostic
phase traces are not release gates. The accepted baseline remains `78c88fb`.

Large real-window reverse/shuffle improve 83.53%/73.77%, but the indexed collection
adds about 0.66 MB of mount allocation at 10,000 rows. Full-list comparisons and
small-workload regressions are retained. No baseline promotion or publication.
