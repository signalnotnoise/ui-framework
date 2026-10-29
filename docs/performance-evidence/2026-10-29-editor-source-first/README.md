# Source-first editor default — October 29, 2026

[Investigation and adoption results](../../editor-mount-investigation-2026-10-29.md).

- `raw-results.zip`: all seven-pair performance results and logs, source/binary
  manifests, paired analysis, default/cold phase checks, conflicting-flag error,
  source audit and final correctness evidence.
- `candidate-source.zip`: measured candidate source and diagnostic/test tools.
- `manifest.json`: verified SHA-256 and byte length for each archived entry.

Source-first is the user-authorized benchmark default on both revisions. Startup
work remains timed and host-first remains an explicit diagnostic. Framework
runtime and consumer startup are unchanged.

The full performance campaign passes 30/32, NOT release eligible. Themed mount
+13.94% and editor mount +10.97% fail the unchanged 10% limit. Neither variability
nor the earlier same-binary improvement clears these failed gates. Correctness
passes: 159 tests, 15 visual checks, 21 stress assertions and startup/edit/dispose.
Default/cold phase order, matching geometry and 1,000 native editors are verified.
Baseline and budgets remain unchanged; prior evidence is retained separately.
