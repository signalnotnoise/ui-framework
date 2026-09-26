# Performance guardrails

Temporary exception approved by the user on September 21, 2026: themed full-list **update allocations** may be up to 7% above the published reference, and themed update time may be up to 12% above the published reference. Across 11-sample paired benchmarks, core list update allocations improved by 5.02% (32 MB saved) and virtualized by 5.84%, while mount allocations and times improved across all scenarios. This accepts the irreducible cost of supporting rich native button content via ButtonContentPresenter (ca428c7) and accessible CheckBox styling (800e1b8). The 2% allocation and 10% time limits still apply to all other scenarios; component-work limits remain at 0% growth.

Performance is a standing repository requirement in [AGENTS.md](../AGENTS.md). For runtime changes, measure before and after, investigate regressions, and pursue improvements while preserving behavior and accessibility.

Run from PowerShell 7 on Windows with the .NET 10 SDK and full Git history:

```powershell
./tools/Test-Performance.ps1
# Use more samples to investigate timing variation:
./tools/Test-Performance.ps1 -Samples 11
# Include 1,000 themed editors in an adaptive grid, 50 edits/layout changes:
./tools/Test-Performance.ps1 -IncludeLayoutEditors
# Also measure the same workloads against the immediate pre-change revision:
./tools/Test-Performance.ps1 -BaselineRef <revision> -IncludeLayoutEditors
```

The layout/editor scenario uses the production
[`WpfComCleanupPolicy`](application-cleanup-policy-2026-09-25.md) on both
revisions. The accepted baseline predates that API, so the runner overlays the
candidate's exact policy source into its isolated baseline tree. This isolates
renderer changes while preserving identical cleanup behavior and workload. The
summary's `releaseEligible` field is false for omitted editor scenarios, fewer
than seven samples, a nonaccepted reference revision, or any exceeded metric
budget. Correctness, lifecycle, package, and consumer checks remain separate.

The runner builds the accepted source revision in an isolated archive and the current working tree in Release mode. Both use the current Counter sample workload. It alternates baseline and candidate processes, discards one warmup process per side and scenario, and records seven measured processes per side by default. Keep the machine otherwise idle during measurement. The default increased from three on September 20 after broad timing ranges produced inconsistent failures; budgets, workloads and the accepted source revision are unchanged. CI and release workflows inherit the larger sample count. A failed run still fails immediately after analysis; there is no automatic retry-until-pass behavior.

Each process uses 1,000 logical rows and 50 deterministic mixed operations. Three scenarios cover the full list, the virtualized list, and the themed full list. The full-list baseline remains mandatory. Initial mount and subsequent updates are measured separately; elapsed time, UI-thread allocated bytes, component body builds, and mount/unmount work are recorded. Correctness assertions include row count and balanced disposal.

The optional layout/editor scenario adds a themed adaptive grid with 1,000 editors, alternating available widths and read-only state, and changing text every five steps. It reports the same mount/update, allocation and body-work fields (there are no component mount/unmount hooks in this workload). Both revisions use the identical current harness. It supplements all three existing scenarios and uses the unchanged default budgets. StateList enumeration continues to use snapshots; the full-list workload includes its materialization cost.

The editor scenario runs on WPF's application dispatcher with a native window
source. Earlier runtime-default runs stalled inside WPF TextStore lock handling,
including on the accepted baseline. The production application-owned policy
retains input methods and the full 1,000-editor/50-operation workload while moving
COM cleanup outside property setters. Do not disable input methods or reduce the
control/operation count to obtain a passing result.

The fixed reference is the published `0.1.0-alpha.1` source, `78c88fb901c202e3c2e49b6de300d1ce2369e00b`. [The budget file](../tools/performance-baseline.json) permits at most 10% median time growth, 2% allocation growth, and no component-work growth. These are initial detection thresholds, not permission to spend performance unnecessarily. Do not weaken them or advance the reference merely to pass. Preserve evidence and explicitly justify any baseline promotion; also compare against the previous revision for each optimization so improvements do not silently erode.

Every run writes raw JSON and logs plus a median comparison with min/max values under a unique `artifacts/performance/` directory. Existing output directories are rejected. A budget failure returns a failing exit code. `-ReportOnly` retains the failure in the report but allows exploratory runs to finish successfully; CI must not use it. `-BaselineRef` allows an additional comparison with a specific prior revision.

Runs also preserve candidate source files and SHA-256 hashes, including untracked files, because a dirty HEAD identifier alone is not reproducible. Each benchmark process has a 120-second timeout; a stall terminates that process tree and fails the campaign while preserving completed raw reports.

Timeouts also write `failure.json` with the affected side, scenario, sample, CPU time and elapsed time. For a separate editor investigation, run the built Counter sample with `--layout-editors --trace-layout --report <path>`; tracing is off in paired runs. The [September 24 investigation](editor-timeout-investigation.md) includes a standalone WPF reproduction and captured text-services waits.

Both GitHub validation and NuGet validation include the editor workload and install `dotnet-dump` version `10.0.745401`. They pass `-DumpToolPath artifacts/diagnostic-tools/dotnet-dump.exe` to the runner. After a process exceeds 120 seconds, the run is already failed; `Save-BenchmarkDump.ps1` then attempts a full dump before terminating that process. Collection has its own 45-second limit and cannot turn a timeout into a passing sample. No collector runs during successful timing samples. The failure metadata includes collection status, and the existing always-run artifact upload includes the `.dmp` and collector log, including unsuccessful captures.

For local capture, install the same tool with `dotnet tool install dotnet-dump --version 10.0.745401 --tool-path artifacts/diagnostic-tools`, then pass the same `-DumpToolPath` argument alongside `-IncludeLayoutEditors`. Inspect a downloaded dump with `dotnet-dump analyze <file.dmp>`, then `threads` and `clrstack -all`. Full dumps support managed stack inspection; mixed native frames may need WinDbg. See the [official tool documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump). Dumps are diagnostic artifacts, not source-controlled performance baselines.

`tools/Test-WorkspacePerformance.ps1 -OutputDirectory <new-directory>` separately compares representative native shell adapters with `Dock`, `FlexColumn` and `SplitPane`. It keeps 1,000 submission rows and a 1,000-line editor mounted through 50 resizing/collapse operations, checks matching geometry and retained selection/ownership, and reports fresh-process alternating samples. It does not exercise text replacement, a complete consumer workspace or unsupported right-pane sizing. It is not a substitute for `Test-Performance.ps1`, and does not change its baseline or budgets.

For an optimization of these primitives, add `-BaselineRef <revision-containing-the-layout-APIs>` to the workspace runner. It then runs the same current primitive workload against that archived framework revision and the working source, rather than using native adapters as the reference. This allows a direct alternating before/after check; do not infer an optimization speedup by subtracting medians from separate adapter campaigns.

For diagnosis, `-Isolation dock`, `column`, or `split` substitutes only that primitive into the native shell; the default `workspace` uses the complete framework composition. The same 1,000 rows, 1,000 editor lines and 50 operations remain, including geometry, selection and ownership assertions. Isolated Dock and FlexColumn descriptions are static while the native split changes size/collapse; isolated SplitPane rebuilds its bound host. These cases distinguish resizing from whole-tree reconciliation, rather than claiming equal body-build counts. Run the full workspace case alongside them.

Add `-ProbeLayout` in a separate campaign to count and time measure/arrange calls at the editor and submission-list boundaries. Probes report unbounded constraints too; they do not count every internal panel pass. Both clean and instrumented modes retain the same two transparent decorators, with counters/timers disabled in clean runs. Probe timings include downstream WPF work and are diagnostic, not a clean latency comparison or release gate. Retain clean and instrumented raw reports separately.

`-CompareSubmissionVirtualization` compares two otherwise identical framework workspaces containing a selectable WPF ListBox: virtualization disabled versus recycling enabled. Both retain all 1,000 logical items, the same row template, editor and 50 resize/collapse operations. It requires the default workspace isolation and cannot be combined with `-BaselineRef`. The original StackPanel workload remains available as the default. Selection retention is checked during updates; scrolling to items 500, 999, 0 and 7, viewport realization and the UI Automation selection pattern are checked separately after timing. Reports include realized-row counts. This measures WPF native-list virtualization through the framework adapter, not the framework's declarative VirtualList implementation or the complete consumer app.

GitHub validation runs this check in a separate Windows job and uploads the evidence, including on failure. The NuGet release workflow also requires a passing comparison before publishing. Commit and push workflow changes to activate these checks remotely; branch protection is a separate repository setting.

Timing remains noisy on shared machines. Inspect all repetitions and repeat with more samples when warranted; keep the failed evidence. Fresh processes still incur JIT work, and warmup primarily primes system caches. Allocations cover the UI thread, not total process memory. These workloads do not measure frame latency, scrolling smoothness, GPU cost, or every control. A passing budget is evidence for these scenarios, not a guarantee against every performance regression. If the sample becomes incompatible with the old framework API, repair the comparison explicitly instead of silently dropping the baseline.
