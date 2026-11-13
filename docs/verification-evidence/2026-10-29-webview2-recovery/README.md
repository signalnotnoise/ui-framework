# WebView2 failure/recovery evidence — October 29, 2026

See [outcomes and limitations](../../webview2-recovery-2026-10-29.md).
`validation.zip` preserves all pilot/build failures, the first aborted repeated
campaign, the final guarded recovery campaign (15/15), original lifetime regression
(15/15), and full framework validation log/TRX/stress/startup reports. Older startup
reports in the shared artifacts folder are context; the recovery validation log
identifies the full run during this change.

The two final campaigns include exact source snapshots verified against their
source-manifest hashes. Pilot manifests/logs record earlier source states but do
not imply final-source identity. Browser profiles, crash dumps/cache and binaries
are excluded from this archive and retained locally. No personal browser data is
included. Each archive entry was re-read and SHA-256 verified against manifest.json.

The invalid-profile provider timeout was not relabelled as a pass: final preflight
rejects that configuration before the provider is called, and the separate
missing-runtime case checks actual provider initialization failure. Generic provider
stall handling remains open. Core/WPF runtime is unchanged; this is local acceptance
and regression evidence, not a fresh performance comparison or hosted release gate.
