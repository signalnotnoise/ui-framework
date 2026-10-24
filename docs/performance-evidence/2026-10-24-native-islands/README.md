# FW-004 evidence — October 24, 2026

See [method, results and limits](../../native-island-performance.md).

Run order was sequential: `revision`, `scope`, `control`, `presentation`, then
correctness validation. Every timing campaign used SDK 10.0.402/runtime 10.0.13,
seven measured processes per side, one discarded warmup per side, alternating
order. No profiler ran during these measurements.

- `revision/`: alpha.5 source fbf5204 against the working tree, the same current
  harness on both revisions, all four existing workloads plus two native modes.
  **Failed full-list update time (+43.29%); 47/48 checks within budget.**
- `scope/`: same binary, intentionally different native style policies. Phase
  timing/allocation and exact ownership assertions; not a release gate.
- `control/`: byte-identical candidate copies, focused full-list workload.
  The archived alpha.1 setup source was built but not executed in this control.
  The apparent -26.41% update difference is variation, not an optimization.
- `presentation/`: the unchanged FW-003 prototype, attached versus unattached.
  Faster updates with higher mount time and allocations; not adopted.
- `revision-source.zip`: exact first campaign candidate source snapshot. It
  predates the later compile-time native guard; native workload body is unchanged.
- `control-source.zip`: guarded candidate snapshot used by the identical-binary
  campaign. Both snapshot manifests are retained with their raw campaigns.
- `tools/`: final runner/analysis sources. `prototype/` preserves the actual
  modified StressRunner and its preparation script. The original source is in
  [FW-003's snapshot](../2026-10-23-timing-investigation/measured-source.zip).
- `correctness.log`, `regression.trx`, `stress.json`: final Release, 119 tests,
  15 visual checks and 21 stress assertions. Measurements do not grant release
  eligibility or replace ordinary accepted-baseline CI.

No accepted baseline/budget or framework runtime source was changed. Exact
binary hashes identify each measured build; source snapshots and final tools
identify differences between measured and subsequently guarded harness inputs.
