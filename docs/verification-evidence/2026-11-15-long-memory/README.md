# FW-018 longer memory evidence - November 15, 2026

See the [findings](../../fw018-long-memory-2026-11-15.md). FW-018 remains open;
these are local ownership and attribution checks, not approved memory budgets
or a hosted performance gate.

`validation.zip` preserves campaign manifests, reports, raw samples, measured-source
snapshots, analysis and logs. `manifest.json` records each entry's size and SHA-256;
every archived entry was reread and verified. The final analysis checks 15 runs
and 2,442 spooled samples. The full Release log records 172 tests, 15 visual checks
and 21 stress assertions passing. Input-observer protocol variants remain separate.

The `fw018-htrace-files-2026-11-15` pilot failed on a collector timeout; its empty
handle report and logs are failure evidence, not acceptance. Its old collector
source snapshot is incomplete. Successful campaigns with measured-sources.zip
have source hashes verified against their recorded manifests. Final collector
source is separately archived, not represented as the earlier measured binary.

Raw dumps and browser profiles are excluded. Local dump paths, sizes and hashes
are recorded separately. Creation stacks were unavailable even in positive controls;
known handle/open/close metadata and file identities passed separately. Window
enumeration does not enumerate all USER/GDI objects. Input correlation does not
establish causation.
