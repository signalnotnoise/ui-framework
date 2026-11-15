# Heap and handle attribution evidence — November 13, 2026

See [findings and limitations](../../fw018-heap-handle-attribution-2026-11-13.md).

`validation.zip` contains the incomplete-handle native pilot, three handle-aware
native/fresh/reused diagnostic campaigns, exact measured source snapshots,
heap/handle summaries, root analyses, source manifests and checkpoint signals.
Raw dumps and browser profiles remain ignored/local and are excluded. Dump sizes
and SHA-256 hashes are retained separately within the archive.

The final normal Release validation and unpaused browser smoke checks are included
separately from paused diagnostics. `manifest.json` records each archived entry's
byte count and SHA-256. No memory budgets or hosted performance gates are claimed.

All 600 archive entries verify against the manifest. All 302 spooled samples in
the four diagnostic runs and two normal browser cases match reconstructed reports.
Normal Release passes 172 tests, 15 visual checks, 21 stress assertions, reporting
controls and window/shutdown checks; both unpaused browser modes and analyzer
fixtures pass. The archive is 1,449,469 bytes; raw memory dumps are excluded.
