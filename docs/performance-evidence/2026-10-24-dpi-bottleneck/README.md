# FW-003 causal evidence — October 24, 2026

See [cause, fix, measurements and limits](../../timing-bottleneck-2026-10-24.md).

- `intervention/`: seven alternating pairs plus discarded warmups, identical
  candidate payloads, cold versus empty-target initialization. All four existing
  workloads retain 1,000 rows/50 operations. Ineligible for release.
- `window-startup/`: seven same-binary actual-window startup pairs, the unchanged
  50-row sample, handle before/after ViewHost construction. Geometry/body work,
  native editing and full disposal match. Ineligible for release.
- `accepted/`: seven pairs against unchanged accepted source 78c88fb; same current
  protocol-v2 harness initializes both revisions, includes initialization in mount,
  and passes all 32 unchanged budgets. Local releaseEligible=true is performance
  evidence only; actual publication still needs exact committed-source CI/packages.
- `profiles/cold/`, `profiles/initialized/`: corrected explicit-argument traces,
  condition/provider checks, matching binary hashes/geometry and phase data.
  Large raw traces, Speedscope files, parsed CLR events and read-only installed
  runtime decompilation (with WPF MIT license) are in `profiles/raw-traces.zip`.
- `causal-analysis.json`: phase CPU, inclusive sampled-frame spans, GC/runtime
  suspensions and actual contention durations from corrected profiles. These are
  diagnostic durations, not replacement timing gates or additive percentages.
- `causal-primed` and `marked-primed` legacy files are INVALID primed comparisons:
  an optional argument was split into characters. Their flags correctly say false.
  Retained for audit, excluded from improvement claims. Earlier dynamic-provider
  runs also lacked observed phase markers. Corrected traces supersede them.
- `measured-intervention-source.zip` and `measured-accepted-source.zip`: exact
  captured inputs for those campaigns. Protocol 1 in the intervention defaults
  cold; protocol 2 in accepted defaults initialized, with explicit cold diagnostic
  mode. Compare within each campaign, not by subtracting independent medians.
- `tools/`, `final-sample/`, `trace-reader/`: final reproducible analysis/runner
  sources and pinned TraceEvent reader project. Final tool copies include stricter
  metadata validation added after measurement; captured snapshots remain unchanged.
- `correctness.log`, `regression.trx`, `stress.json`, `startup-*.json`: zero-warning
  Release, 119 tests, 15 visual checks, 21 stress assertions and startup integration.
- `runtime-info.json`: SDK/runtime/tool versions and exact installed WPF DLL hashes.

Windows machine, SDK 10.0.402/runtime 10.0.13. All timing processes/campaigns were
sequential; profilers ran separately. Initial pilot/smoke runs are diagnostic,
not used as repeated performance evidence. The machine was not exclusively
reserved. SystemAware/root-DPI equivalence is verified here; broader physical
DPI/assistive-technology acceptance remains separate. No accepted revision,
budget, data size or operation count was relaxed; old failed gates stay failed.
