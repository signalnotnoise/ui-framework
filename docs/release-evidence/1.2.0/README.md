# Version 1.2.0 acceptance evidence

Published November 20, 2026 (America/New_York), from
`5be84d62fca50c25d1bdf582a4ee704e9f87739b`.

[acceptance.zip](acceptance.zip), SHA256 `fa74f8d5ac33a66d41a333a003d545c5c88fd5d30f6f06e510add9e0b9e81ba0`.

The archive contains raw local and both hosted full performance comparisons,
source/binary manifests, local/public 1.1.0-to-1.2.0 upgrade campaigns, hosted
release-validation reports, package manifests, package-only feature smoke source
and results, public artifact equality checks, and publication/validation logs.
The source itself is retained by the release commit and tag; source archives and
package binaries are not duplicated here. Public NuGet signatures may change ZIP
hashes, so acceptance compares embedded DLL and README bytes and repository metadata.

Each full comparison passes 32/32 unchanged-budget checks against `78c88fb`.
Local and public upgrade campaigns each pass 14 processes, including unchanged
binary consumers. Upgrade timing remains diagnostic, not release-gate evidence.
The publication log records acceptance of both libraries and both symbol packages.
[Release details](../../releases/1.2.0.md).
