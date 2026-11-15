# FW-012 release/documentation reconciliation — November 15, 2026

FW-012 is complete. Current guides now separate the published alpha.7 package,
unpublished working-tree changes, completed scoped acceptance and future exact-RC
gates. No framework source, package version, performance budget or Git branch was
changed by this documentation task. Nothing was published or merged.

## Recovered release history

The checkout retained the alpha.7 preparation record but lacked the completed
record and evidence already committed in `42d66173ef864d02a3e135b59284b43e7033a1e0`.
The release record and 601 evidence files were selectively restored from that
commit, without checking out or merging unrelated code. Existing working changes
were preserved. The [alpha.7 release record](releases/0.1.0-alpha.7.md) links the
restored local checks, failed first public restore, publication log, package hashes
and raw repeated hosted/local comparisons.

Read-only GitHub inspection confirms:

- [Validate 43409502188](https://github.com/signalnotnoise/ui-framework/actions/runs/43409502188)
  succeeded for exact source `c5d639bcff707ff1a75b23f2a60135d6be6cb7b2`.
- [Publish NuGet 43410864931](https://github.com/signalnotnoise/ui-framework/actions/runs/43410864931)
  succeeded for the same source, including its publication job.
- Both preserved performance summaries pass 32/32 comparisons against unchanged
  baseline `78c88fb901c202e3c2e49b6de300d1ce2369e00b`. The newly downloaded publishing
  performance summary is byte-identical to the restored record.
- The public-installation DLL hashes match the recent independent FW-019 alpha.7
  consumer. The recorded hosted/public payload match is therefore corroborated by
  a fresh isolated public installation, not merely the NuGet version listing.

FW-024 is complete on that verified publication history. Later November 13 Validate
runs for `ab480ad` and `dd01bdd` failed; those different commits do not invalidate
the published candidate's successful runs and do not establish that today's code
passes. No failed run was retried or repaired in this task. A new candidate needs
its own exact-source gates.

## Current-facing corrections

README, changelog and release instructions now install alpha.7 and state its
published status. Unreleased notes identify the subsequent peer-cache cleanup
and acceptance tooling; those are not retroactively attributed to alpha.7.

Layout/workspace guides no longer describe vertical fill, docking, split panes
or toggle styling as unavailable source-only work. The API/accessibility guides
identify shipped discovery/wrapper/editing behavior while retaining the unpublished
peer-cache cleanup boundary. Native-interop documentation distinguishes the initial
fixture's limitations from completed scoped FW-010 and FW-018 acceptance.

The roadmap is labelled as virtualization design history, not an unimplemented
next milestone. The backlog remains authoritative for work status. Historical
manual IME/contrast/scaling acceptance is credited without inventing build/provider
details; the agreed post-v1 diagnostics/Narrator work stays deferred. Original dated
failures and release-time pending statements remain historical records, with later
completion links where needed.

Release instructions distinguish the verified Windows 11 x64 / .NET 10.0.13 sample
environment from a broader untested support promise, explain the stable .NET 10
SDK requirement, and require a unique new package plus exact candidate acceptance.
The alpha.6-to-alpha.7 adoption comparison does not cover unpublished source changes.

## Verification

Read-only run metadata and the reconciliation assertions are retained under
[verification evidence](verification-evidence/2026-11-15-release-reconciliation/).
The curated and generated graphs, documentation-version/action-pin checks and
diff whitespace checks are validated after the edits. No new runtime benchmark
is required for these documentation changes, and no new runtime test pass is claimed.
