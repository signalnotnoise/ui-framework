# Real WebView2 evidence — October 29, 2026

See [case results and limitations](../../webview2-acceptance-2026-10-29.md).

`validation.zip` contains the two failed environment-pending pilots, the corrected
five-case pilot, intermediate repeated runs and the final `owned-windows` campaign
(15/15), plus full Release validation output and test/stress/startup reports.
Earlier startup/native-window reports in the shared artifacts folder are retained
as context; `webview-release-validation.log` identifies the current regression run.
Final and intermediate measured-source ZIPs preserve source manifests and matching
framework/sample source. The final source is based on `64bd242` plus uncommitted
acceptance tooling; no core/WPF runtime code changed.

`manifest.json` records SHA-256 and length for each archive entry. Archive contents
were re-read and hashes verified. Browser profile/cache files and compiled binaries
are excluded. Each isolated profile is retained locally under the corresponding
`artifacts/webview-acceptance-2026-10-29` case folder.

The original environment-provider race assertion failed because environment creation
completed synchronously. The replacement is explicitly an application-boundary test;
pending control and popup-child initialization are still required and observed.
Reports are local acceptance evidence, not a hosted CI gate, browser memory budget,
or validation of GuidedGrade's current lifecycle code.
