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

The runner builds the accepted source revision in an isolated archive and the current working tree in Release mode. Both use the current Counter sample workload. It alternates baseline and candidate processes, discards one warmup process per side and scenario, and records seven measured processes per side by default. Keep the machine otherwise idle during measurement. The default increased from three on September 20 after broad timing ranges produced inconsistent failures; budgets, workloads and the accepted source revision are unchanged. CI and release workflows inherit the larger sample count. A failed run still fails immediately after analysis; there is no automatic retry-until-pass behavior.

Each process uses 1,000 logical rows and 50 deterministic mixed operations. Three scenarios cover the full list, the virtualized list, and the themed full list. The full-list baseline remains mandatory. Initial mount and subsequent updates are measured separately; elapsed time, UI-thread allocated bytes, component body builds, and mount/unmount work are recorded. Correctness assertions include row count and balanced disposal.

The optional layout/editor scenario adds a themed adaptive grid with 1,000 editors, alternating available widths and read-only state, and changing text every five steps. It reports the same mount/update, allocation and body-work fields (there are no component mount/unmount hooks in this workload). Both revisions use the identical current harness. It supplements all three existing scenarios and uses the unchanged default budgets. StateList enumeration continues to use snapshots; the full-list workload includes its materialization cost.

The editor scenario runs on WPF's application dispatcher with a native window source. On September 23, its initial headless version and subsequent window/dispatcher probes stalled inside WPF TextStore lock handling on this machine, including on the accepted baseline. The workload is retained as an optional diagnostic; there is no valid paired layout/editor latency result yet. See the [review resolution](review-resolution-2026-09-23.md). Do not disable input methods or reduce the control/operation count to turn that stall into a passing result.

The fixed reference is the published `0.1.0-alpha.1` source, `78c88fb901c202e3c2e49b6de300d1ce2369e00b`. [The budget file](../tools/performance-baseline.json) permits at most 10% median time growth, 2% allocation growth, and no component-work growth. These are initial detection thresholds, not permission to spend performance unnecessarily. Do not weaken them or advance the reference merely to pass. Preserve evidence and explicitly justify any baseline promotion; also compare against the previous revision for each optimization so improvements do not silently erode.

Every run writes raw JSON and logs plus a median comparison with min/max values under a unique `artifacts/performance/` directory. Existing output directories are rejected. A budget failure returns a failing exit code. `-ReportOnly` retains the failure in the report but allows exploratory runs to finish successfully; CI must not use it. `-BaselineRef` allows an additional comparison with a specific prior revision.

Runs also preserve candidate source files and SHA-256 hashes, including untracked files, because a dirty HEAD identifier alone is not reproducible. Each benchmark process has a 120-second timeout; a stall terminates that process tree and fails the campaign while preserving completed raw reports.

GitHub validation runs this check in a separate Windows job and uploads the evidence, including on failure. The NuGet release workflow also requires a passing comparison before publishing. Commit and push workflow changes to activate these checks remotely; branch protection is a separate repository setting.

Timing remains noisy on shared machines. Inspect all repetitions and repeat with more samples when warranted; keep the failed evidence. Fresh processes still incur JIT work, and warmup primarily primes system caches. Allocations cover the UI thread, not total process memory. These workloads do not measure frame latency, scrolling smoothness, GPU cost, or every control. A passing budget is evidence for these scenarios, not a guarantee against every performance regression. If the sample becomes incompatible with the old framework API, repair the comparison explicitly instead of silently dropping the baseline.
