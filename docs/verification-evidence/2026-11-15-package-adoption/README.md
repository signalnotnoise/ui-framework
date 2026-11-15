# Independent package adoption evidence

[Scope, results and reproduction](../../fw019-package-adoption-2026-11-15.md).

`validation.zip` preserves failed pilots, the intermediate campaign, and the final
14-process campaign. `final/summary.json` contains per-process medians, source and
binary upgrade provenance validation, signed metric differences and post-shutdown
memory ranges. Adjacent reports/captures/logs retain each process's raw observations.
Assets and lock files identify NuGet-only resolutions; assembly manifests and loaded
hashes verify the binary swap. Archived `.nupkg` files preserve both exact versions.

`sources/` contains the standalone sample and driver/analyzer. Both measured
external consumers' C# files were compared byte-for-byte with this sample before
archiving. The driver was subsequently wired to invoke the same summarizer that
was run explicitly for this campaign; measurement source/workload is unchanged.
The standalone README was added after measurement. Both historical harness pilots
are described in the dated report; they are not passing package results.

`manifest.json` hashes all 137 archived entries and the zip; every entry was reread
and verified. Scratch paths in campaign/restore reports identify retained external
consumers and isolated caches. This archive is local acceptance evidence, not a
hosted performance gate, universal memory budget or v1 publication approval.
