# Reporting retention evidence — November 13, 2026

See [method, results and limits](../../fw018-reporting-retention-2026-11-13.md).

`validation.zip` preserves the three 60-batch native runs, their complete original
sample sidecars, the failed browser pilot and six corrected browser runs, per-cycle
reports, logs, manifests and exact measured source snapshots. Isolated browser
profiles remain local; they are not included in this archive. `manifest.json`
records and verifies each archive entry's size and SHA-256.

It also preserves the instrumentation controls, initial Release log (172 tests,
15 visual checks, 21 stress assertions and window/shutdown checks), TRX, stress
report, direct fresh-directory smoke check and current tooling sources. The native
campaign snapshot predates only the CLI's early report-directory creation fix;
the initial direct invocation verifies directory creation but fails sample-root
isolation (one survivor). It is not a successful isolation check. Measured probe/workload code is
unchanged. Browser pilot and corrected campaign sources are stored separately.

Native reporting deltas are 31,808 bytes in all three runs. Corrected browser deltas
are 38,016 bytes in each fresh-profile run and 25,344 in each reused-profile run.
All corrected runs collect every reporting sample root. The failed pilot retains
one root and is preserved as failed isolation even though browser ownership passes.

`final-validation.zip` and `final-manifest.json` preserve the follow-up: native
sample construction moved out of the loop, and both callers reject reporting-root
survivors. Final three 60-batch native runs pass, releasing 31,808 / 31,832 / 31,808
bytes with zero sample survivors. Two final browser guard runs pass with the same
38,016 / 25,344 per-mode deltas. Final Release again passes 172 tests, 15 visual
checks, 21 stress assertions, reporting and window checks, plus the newly included
one-batch native regression (zero survivors). Source snapshots match the final
campaign manifests. Both archives' entries were checked against their hashes.

These are sample-tool managed-memory diagnostics, not new framework performance
gate results, adjusted heap trends, approved budgets or a package release.
