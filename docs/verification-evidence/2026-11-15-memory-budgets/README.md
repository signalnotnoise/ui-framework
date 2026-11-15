# Scoped memory-budget evidence — November 15, 2026

[Findings and scope](../../fw018-memory-budgets-2026-11-15.md).

`validation.zip` preserves all 12 passing fresh-process runs, raw bounded samples,
campaign/source manifests, logs, fixture results, historical calibration failures,
the pre-run frozen policy and its evaluation. `summary.json` inside the archive
contains maxima across processes; `budget-results.json` retains every individual
comparison. `manifest.json` hashes every entry and the archive itself; the archive
was reread and verified after writing.

`measured-sources.zip` inside the archive preserves final framework/sample/evaluator
sources. Campaign manifests identify measured sources. The final driver additionally
rejects diagnostic environment overrides; that guard was added during the campaign.
The recorded environment observation has no overrides. Policy numerical limits
are identical to the frozen copy; only validation status changes after success.

SDK 10.0.402 / .NET 10.0.13 / WebView2 157.0.4341.46. The 58 framework source entries
match the preceding peer-cache test/performance candidate. Its 176 tests and 32/32
local performance checks are prior evidence, not freshly rerun by this campaign.
No hosted release gate is claimed. Browser profiles and process dumps are excluded.

One fresh-profile browser run reaches the +16 late-handle limit exactly. The older
+20 late-handle calibration outlier remains a failure under the same policy.
Physical Narrator revalidation and additional diagnostics are deferred post-v1
items FW-026 and FW-025, not completed checks.
