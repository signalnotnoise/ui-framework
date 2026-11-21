# Vector icon verification — November 20, 2026

Unreleased FW-037 work on `codex/vector-icons`. Source baseline is
`04ce2e9a0ab2eab9f26137aaa8e2b103ccfe7554`; the candidate was dirty and its
exact runtime source snapshot, hashes, binaries' hashes and raw runs are archived.
Accepted release baseline `78c88fb` and all budgets remain unchanged.

## Evidence

[local-evidence.zip](local-evidence.zip), SHA256 `2232b23e9e1630181deb9a22f41b7e55f173939270ce408d2adae22f67b66120`.
Includes raw alternating benchmark runs, candidate source, final 206-test TRX,
preview source and inspected wide/compact light/dark WPF captures at 144 DPI.
Ten icon tests cover geometry/cache lifetime, retention, invalid input,
automation, native invocation/disabled/focusability, theme/contrast inheritance,
wrapping and filled/outlined pixel checks at 96/144/192 DPI.

Commands from the repository root (SDK 10.0.402, runtime 10.0.13):

```powershell
./tools/Test-Performance.ps1 -BaselineRef 04ce2e9 -Samples 7 -OutputDirectory artifacts/icons/performance
dotnet test 'tests/UI Framework.Checks/UI Framework.Checks.csproj' -c Release --logger 'trx;LogFileName=final.trx' --results-directory artifacts/icons/tests
dotnet run --project artifacts/icons/preview/IconPreview.csproj -c Release
```

The preview source and project can be restored from the archive to the same
`artifacts/icons/preview` path. Preview screenshots are actual WPF renders;
they do not establish physical Narrator acceptance.

## Results

All 24 local comparisons pass after seven alternating measured pairs plus one
warmup process per side, for full-list, virtualized and themed full-list workloads
(1,000 rows, 50 operations). These quantify the ordinary path's overhead, not
the cost of rendering an icon-heavy application or a hosted release gate.

- Full-list mount/update time: +0.05% / -0.15%; UI-thread allocations +0.09% / +0.34%.
- Virtualized mount/update time: -1.02% / -0.21%; allocations +0.30% / +1.04%.
- Themed full-list mount/update time: -0.79% / -1.44%; allocations +0.24% / +0.10%.
- Component body work, mounts and unmounts are unchanged in all three workloads.

Timing differences are small and should be treated as noise, not a demonstrated
speedup. Allocation growth is consistent with one optional reference on each
view description and stays within existing budgets. No baseline promotion,
new performance exception, package publication or hosted acceptance is claimed.

Initial focused testing caught automation-peer recursion when querying the public
peer name API from a core override; using the protected core getter corrected it.
The final regression suite includes that case and cache-collection coverage.
