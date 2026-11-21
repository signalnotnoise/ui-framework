# Shared design-token verification — November 20, 2026

FW-038 is source-only and unreleased. The before revision is the local benchmark
snapshot `980618d6470c5ad0210298fb3b3d0b2420b52c37`, containing the preceding
uncommitted icon work. It was created with `git write-tree` / `git commit-tree`
without advancing a branch. The candidate remains dirty; its archived runtime
source snapshot and manifests identify what was measured. Accepted release
baseline `78c88fb` and all budgets remain unchanged.

[local-evidence.zip](local-evidence.zip), SHA256 `c83cc04ccc5ff1cf84701128961abdd242e0fb46bf0dd148acb14ca024144644`.
The archive preserves raw repeated runs, source/binary manifests, candidate source,
baseline core/WPF/Counter source subset, 212-test TRX, compiled skill recipe source,
preview source and native captures. The baseline source subset is included because
the local snapshot object may eventually be garbage-collected; it is not a complete
repository archive. Baseline and candidate use the same benchmark harness.

## Commands and scope

SDK 10.0.402 / runtime 10.0.13, same machine and workload, seven alternating measured
pairs plus one warmup process per side. Each workload has 1,000 rows / 50 operations.

```powershell
./tools/Test-Performance.ps1 -BaselineRef 980618d6470c5ad0210298fb3b3d0b2420b52c37 -Samples 7 -OutputDirectory artifacts/design-tokens/performance
dotnet test 'tests/UI Framework.Checks/UI Framework.Checks.csproj' -c Release --results-directory artifacts/design-tokens/tests --logger 'trx;LogFileName=regression.trx'
dotnet build artifacts/design-tokens/recipe/Recipe.csproj -c Release -warnaserror
dotnet run --project artifacts/design-tokens/preview/TokenPreview.csproj -c Release
```

Restore preview/recipe source to their archived `artifacts/design-tokens` paths
before running those commands. The test run passes all 212 checks, including six
new token tests and ten icon tests. Coverage includes every numeric token's invalid
values, zero-size rules, immutable default sharing, explicit composition and
retained/nested scoped icon-gap updates. The skill recipe builds with zero warnings.

## Performance

All 24 local comparisons pass. Full-list and themed controls remain in the campaign.

- Full-list mount/update time: -0.37% / -2.97%; UI-thread allocations +0.01% / +0.13%.
- Virtualized mount/update time: -1.81% / -0.27%; allocations approximately unchanged (both within 0.01%).
- Themed full-list mount/update time: -0.96% / -4.44%; allocations -0.03% / +0.14%.
- Component builds, mounts and unmounts are unchanged.

These are local diagnostic comparisons, not a hosted release gate or proof of a
speedup. Small timings remain noisy. No budget or accepted baseline was changed.
The campaign covers ordinary/themed workloads, not a separate icon-heavy stress test.

## Visual checks

Four default light/dark wide/compact PNGs match the preceding icon previews byte
for byte after replacing literals with tokens. Customized spacing, typography,
icon size and label gap were rendered and inspected at wide/compact widths.
An initial enlarged compact capture exceeded the fixture's fixed height; that pilot
is preserved. The capture viewport was expanded for the enlarged theme, with no
runtime/layout or workload change. Final content is visible and long labels wrap.
No new physical Narrator or manual high-contrast acceptance is claimed.
