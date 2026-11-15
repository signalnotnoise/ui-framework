# GuidedGrade-closed native resource evidence — November 15, 2026

See [findings](../../fw018-guidedgrade-closed-resources-2026-11-15.md).
FW-018 remains in progress; this is paused diagnostic evidence, not a performance
release gate or approval of memory budgets.

The archive retains reports, raw samples, process/input audits, native object
inventories, decoded kernel-handle metadata, test logs and source snapshots.
Full memory dumps remain local; their sizes and SHA-256 hashes are in the manifest.
Every archived entry is reread and checked against its recorded hash.

Accepted comparison runs are `manual`, `control`, `thread-exit` and
`thread-control-untouched`. The last two use identical sample/collector binaries
and SDK. Each accepted run has 603 samples and 2,412 workspace cycles, including
three warmup batches. Original ownership, reporting and shutdown checks apply.

Two excluded attempts are preserved separately: `thread-control` detected
GuidedGrade at the final exit audit; `thread-control-clean` recorded activation
and pointer input and was stopped at the user's request. Neither contributes
to the accepted comparison totals. Their partial evidence is not a passing run.

The archived 172-test result belongs to the preceding source-identical diagnostic
session. Native memory and dump-parser fixtures were rerun successfully during
the clean-control continuation. No framework library change or hosted gate is
claimed in this continuation.
