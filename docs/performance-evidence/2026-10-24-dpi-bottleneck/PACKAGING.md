# Evidence packaging for the October 24 commit

The six large top-level Speedscope JSON exports were verified byte-for-byte
against their existing entries in `profiles/raw-traces.zip`, then their duplicate
working copies were moved to ignored local artifacts. Raw ZIP contents and every
benchmark result are unchanged. No failed evidence was discarded.

`archive-manifest-before-packaging.json` preserves the original layout/hashes.
In `archive-manifest.json`, records with an `entry` identify a ZIP member; hash
the decompressed member to verify that record. Other records remain physical
file hashes. This consolidation avoids committing/scanning over 130 MB of
redundant derived JSON alongside the same preserved compressed evidence.
