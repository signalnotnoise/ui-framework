# WebView2 shutdown acceptance evidence — November 11, 2026

`validation.zip` preserves the initial warnings-as-errors build failure, 4/4 pilot,
42/42 repeated acceptance reports and process logs, full Release regression log,
TRX and stress/startup/native-window reports. `manifest.json` records each entry's
size and SHA-256; every archived entry was re-read and verified. The tested source
snapshot was checked against the repeated campaign's source manifest before packing.
The earlier build failure predates the explicit task-discard correction.

Base commit 64bd242 with uncommitted sample/tests/docs. SDK 1.0.4314.35, WebView2
runtime 157.0.4341.42 and .NET 10.0.13. Browser profiles, caches and dumps are excluded.
The four new cases include controlled pending-task tests, not induced provider
hangs. This archive is not a performance release gate or GuidedGrade acceptance.
See [policy and results](../../webview2-shutdown-2026-11-11.md).
