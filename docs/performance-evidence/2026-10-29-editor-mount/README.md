# Editor mount evidence — October 29, 2026

[Investigation and interpretation](../../editor-mount-investigation-2026-10-29.md).

- `raw-results.zip`: exact FW-007 hosted run metadata/logs/performance artifact;
  local phase and seven-pair untraced comparisons; focused default-harness check;
  release validation, analysis and source/binary audit.
- `profiles.zip`: both EventPipe traces, sampled stacks, runtime events, reports,
  phase telemetry and binary manifests.
- `candidate-source.zip`: diagnostic candidate framework/sample/test/tool sources.
- `manifest.json`: per-entry SHA-256 and byte counts, verified after writing.

Hosted 32/32 success applies to commit `5fd4a2a`. The local initialization-order
experiment is diagnostic, not a release gate. Source-first improves the mount
median 8.43% with 0.60% fewer mount allocations; update time/allocation increase
2.00%/0.87%. Default order remains host-first. All startup costs are timed; traced
geometry matches. No framework runtime or accepted baseline changes are included.
The focused 8/8 run overlays the same current sample harness onto both revisions;
its large timing variation does not measure a new framework optimization.
