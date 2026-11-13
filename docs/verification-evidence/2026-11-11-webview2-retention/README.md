# Browser retention evidence — November 11, 2026

`validation.zip` preserves the short pilot, preliminary combined 48-case campaign,
corrected six-run retention campaign, every per-cycle measurement, full Release
regression log/TRX/stress/startup/native-window reports, and source manifests.
Each final run has 44 normal cycles (four warmup, 40 measured) plus one negative
control. `retention-analysis.json` summarizes counts and first/last/min/max metrics.

The preliminary combined campaign's summary mislabeled 44 actual cycles as 24;
its per-cycle files and original tested-source.zip are preserved unchanged.
The final source uses one constant for loop/report counts and records negative
control before/after evidence. The final source snapshot was verified against its
campaign source-manifest before archiving. The 42 existing non-retention cases pass
in the combined campaign; their source is unchanged by the reporting correction.

Every archive entry was re-read and SHA-256 checked against `manifest.json`.
Browser profiles, caches, dumps and binaries are excluded. The archive is not a
performance release gate or GuidedGrade acceptance. Base 64bd242 plus uncommitted
sample/tests/docs; SDK 1.0.4314.35, runtime 157.0.4341.42, .NET 10.0.13.

See [results, limitations and exception-test review](../../webview2-retention-2026-11-11.md).
