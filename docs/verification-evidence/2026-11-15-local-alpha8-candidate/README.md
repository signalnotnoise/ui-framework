# Local alpha.8 candidate verification

[Results, provenance and limits](../../fw027-local-candidate-2026-11-15.md).

`validation.zip` preserves full Release/API/package logs, fresh test outputs,
memory/graph fixture results, initial framework manifest, packaged framework
source zip, source-match proofs, all four local library/symbol packages and hashes.
The performance directory preserves seven alternating pairs plus warmups across
all four standard scenarios, binary/source manifests and the exact candidate
benchmark sources. Baseline source/build output is omitted from this archive;
the immutable baseline commit is alpha.7's `c5d639b`, and the baseline source zip
is retained in the original local artifacts directory.

`package-upgrade/` preserves all 14 source/binary consumer runs, captures, isolated
restore/build logs, assets/lock files, loaded assembly hashes, verified local-package
hashes and diagnostic summary. The installed local package archives match the
files under `packages/` exactly. Consumer source is also in the performance source
snapshot. `manifest.json` hashes every entry and the archive; entries were reread
and verified after creation.

Both comparisons are against alpha.7, not the unchanged accepted release baseline.
No hosted gate, publication or new physical accessibility acceptance is claimed.
API guard logs include expected compiler rejections before baseline restoration.
The older 12-run scoped memory evidence is reused only after framework source-hash
verification; this task does not claim a new long soak. Smaller workspace timing
increases remain recorded even though the full 32-metric comparison passes.
