# Real-window and sustained-memory evidence — October 29, 2026

See [findings and limitations](../../native-window-memory-2026-10-29.md).

`validation.zip` preserves the initial window/memory/retention pilots, both repeated
campaigns, their process logs, raw JSON, source/binary manifests and frozen measured
source ZIPs. It also contains the full Release validation log, test TRX, stress report,
screenshot and startup/native-window reports. Older startup reports in that artifacts
folder are retained as context; the full validation log identifies this run.
`manifest.json` records SHA-256 for every archive entry; archive bytes were verified.

The base revision is `1c4cb55`; measured source includes uncommitted sample/tooling
changes and the earlier native-lifetime tests. Both campaign source archives preserve
exact measured framework/sample files. No framework runtime or API changed.

Release validation passed 172 tests, 15 visual checks, 21 stress assertions,
startup/editing/disposal and real-window ownership/shutdown. The longer soak passed
three repetitions, 756 workspace cycles and 57,456 native mounts including warmup.
The analyzer's negative-control/regression fixtures also pass. These are local
correctness/memory observations, not an exact-candidate GitHub performance gate.
