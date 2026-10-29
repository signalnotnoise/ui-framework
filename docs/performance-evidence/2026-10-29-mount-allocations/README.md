# FW-007 mount allocations — October 29, 2026

See [the investigation](../../fw007-mount-allocations-2026-10-29.md) for ownership,
Git history findings, rejected experiments, results and limitations.

- `candidate-source.zip`: final framework, tests, sample, supplemental harness and comparison scripts.
- `raw-results.zip`: raw JSON, stderr/process logs, TRX, final release checks, source audit and archival/summarization scripts.
- `intrusive-probe-source.zip`: first embedded-link prototype; residual mount overhead remained.
- `layout-phase1-source.zip`: storage/initial-buffer prototype; about 54 KB overhead remained at 10,000 rows.
- `final-layout-phase1-source.zip`: rejected balanced-build/initial-Reset prototype. The folder's historical “final” label does not mean accepted.
- `insert-batch-probe-source.zip`: rejected insertion-path variant of batch initialization.
- `phase-diagnostic-source.zip`: identical diagnostic phase/GC additions on guarded and batched sources.
- `manifest.json`: SHA-256 and byte length for all 1,968 archived entries, verified after writing.

The final supplemental folders are `accepted-layout-guard`, `accepted-layout-phase1`
and `accepted-refresh-phase1`; “accepted” identifies the retained local candidate,
not a promotion of the accepted performance baseline. Seven measured alternating
pairs plus warmup use SDK 10.0.402/runtime 10.0.13. Original phase-one and guarded
baselines remain in the October 28 indexed/guarded source archives.

`performance` is the broad alpha.6 comparison: 31/32 checks, with an editor mount
time failure. `editor-identical-enabled` is a byte-identical control, not a source
comparison; `editor-repeat-enabled` is the separate source confirmation, passing
8/8. All three are ineligible for public-release approval. Their 96 processes
completed with maximum wall time 28.17 seconds. No result was discarded. The
incomplete `editor-identical` setup omitted the optional-workload enabling switch;
it was stopped before measurement and is explained in setup-notes.log.

Final correctness: 159 tests, 15 visual checks, 21 stress assertions and actual-
window startup/editing/disposal. All runtime/source audit comparisons match.
The accepted baseline remains 78c88fb; budgets and package versions are unchanged.
