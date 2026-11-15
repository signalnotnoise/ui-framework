# Automation peer-cache cleanup evidence — November 15, 2026

See [findings](../../fw018-peer-cache-cleanup-2026-11-15.md).

The archive preserves the pre-change native retention report, three final candidate
reports (21 cases), intermediate reports and failed test attempts, the passing
176-test suite, and four expected baseline regression failures. Baseline sensitivity
uses copied final tests with isolated pre-change core/WPF binaries; normal test output
was not overwritten with baseline libraries.

The final performance comparison uses seven alternating measured pairs against
pre-change `c5d639bcff707ff1a75b23f2a60135d6be6cb7b2`, with full-list, virtualized,
themed-full-list and layout-editor workloads. Raw measurements, process timings,
summary, measured candidate source archive and source manifests are included.
The earlier stopped performance attempt is intermediate/excluded evidence.
The accepted release baseline remains `78c88fb901c202e3c2e49b6de300d1ce2369e00b`;
this pre-change comparison is not the exact-commit hosted release gate.

Each archive entry has a byte length and SHA-256 in `manifest.json` and is reread
and hash-verified after creation. Source snapshots include the final renderer and
new regression test. No full memory dumps or browser profiles are included.

Automated provider checks do not replace physical Narrator acceptance or establish
a budget for sustained external screen-reader queries. Representative memory budgets
and contextual failure diagnostics remain separate FW-018 work.
