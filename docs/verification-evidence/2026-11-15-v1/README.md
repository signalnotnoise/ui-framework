# Version 1.0.0 release evidence

Candidate: `cbd85b8beb2e3aa57b709dd96b4943365db02f57`.
See [the detailed release record](../../releases/1.0.0.md).

`candidate-validation.zip` retains 605 raw candidate-validation entries: local
Release/package/source-binary upgrades, local accepted-baseline performance,
231-source input verification, hosted Validate evidence and its package artifacts.
`candidate-manifest.json` records the archive hash, byte size and scope. Raw files
are compressed without newline conversion. `publication-validation.zip` and its manifest retain 344 publication-workflow
entries including both library/symbol artifacts and push receipts.
`public-installation.zip` and its manifest retain fresh public upgrade/cleanup
checks, installed-versus-uploaded DLL hashes and sample locked restore.
`public-installation.json` exposes the hash comparison directly.
[Validate performance](validate-performance.md) and
[publication performance](publish-performance.md) each passed all 32 checks.

The source comparison uses seven alternating pairs and the unchanged accepted
baseline. Independent workspace measurements use three alternating pairs and
remain diagnostics, not an additional release performance budget. Historical
long memory/manual evidence is reused within its documented scope, not rerun
or presented as new physical interaction acceptance.
