# Automation peer attribution evidence - November 15, 2026

See [findings](../../fw018-automation-retention-2026-11-15.md).
All archive entries have sizes and SHA-256 values in manifest.json and were reread
and verified after compression. Local raw dumps are not included.

The archive preserves 21 final peer cases, six alternating 180-batch soaks and
1,098 raw samples, the successful 183-batch focus control and 60 samples preceding
the rejected activation. It also contains regression TRX (172 passed), parser and
negative-control logs, historical dump type summaries, intermediate pilot evidence,
and source/binary manifests. No performance release gate or memory budget is approved.

soak-sources.zip describes the six alternating soaks; focus-sources.zip describes
the later focus build and final peer cases. Earlier pilot builds are labeled
separately and are not represented by final source hashes. The earlier interaction
summary omitted activation/focus and is retained alongside its corrected version.
analyze-focus.py describes an uncompleted four-run plan; only the first control
completed and the first focus case failed. No completed focus comparison is claimed.

Historical dump hashes remain in the preceding
[long-memory evidence](../2026-11-15-long-memory/README.md). The actual ref-counted
ElementProxy root is preserved in the earlier
[screenshot comparison](../2026-11-15-snipping-comparison/README.md).
The managed peer-root experiment models that downstream reference graph; it does
not identify the external COM client. No user screenshot is redistributed here.
