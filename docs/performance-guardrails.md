# Performance guardrails

Temporary exception approved by the user on September 18, 2026: themed full-list **update allocations** may be up to 6% above the published reference. This accepts the remaining cost of the readable toggle template for now. The 2% allocation limit still applies to mounts and other scenarios; time and component-work limits are unchanged. Keep the failed original reports and continue trying to reduce this overhead.

Performance is a standing repository requirement in [AGENTS.md](../AGENTS.md). For runtime changes, measure before and after, investigate regressions, and pursue improvements while preserving behavior and accessibility.

Run from PowerShell 7 on Windows with the .NET 10 SDK and full Git history:

```powershell
./tools/Test-Performance.ps1
# Use more samples to investigate timing variation:
./tools/Test-Performance.ps1 -Samples 7
```

The runner builds the accepted source revision in an isolated archive and the current working tree in Release mode. Both use the current Counter sample workload. It alternates baseline and candidate processes, discards one warmup process per side and scenario, and records three measured processes per side by default. Keep the machine otherwise idle during measurement.

Each process uses 1,000 logical rows and 50 deterministic mixed operations. Three scenarios cover the full list, the virtualized list, and the themed full list. The full-list baseline remains mandatory. Initial mount and subsequent updates are measured separately; elapsed time, UI-thread allocated bytes, component body builds, and mount/unmount work are recorded. Correctness assertions include row count and balanced disposal.

The fixed reference is the published `0.1.0-alpha.1` source, `78c88fb901c202e3c2e49b6de300d1ce2369e00b`. [The budget file](../tools/performance-baseline.json) permits at most 10% median time growth, 2% allocation growth, and no component-work growth. These are initial detection thresholds, not permission to spend performance unnecessarily. Do not weaken them or advance the reference merely to pass. Preserve evidence and explicitly justify any baseline promotion; also compare against the previous revision for each optimization so improvements do not silently erode.

Every run writes raw JSON and logs plus a median comparison with min/max values under a unique `artifacts/performance/` directory. Existing output directories are rejected. A budget failure returns a failing exit code. `-ReportOnly` retains the failure in the report but allows exploratory runs to finish successfully; CI must not use it. `-BaselineRef` allows an additional comparison with a specific prior revision.

GitHub validation runs this check in a separate Windows job and uploads the evidence, including on failure. The NuGet release workflow also requires a passing comparison before publishing. Commit and push workflow changes to activate these checks remotely; branch protection is a separate repository setting.

Timing remains noisy on shared machines. Inspect all repetitions and repeat with more samples when warranted; keep the failed evidence. Fresh processes still incur JIT work, and warmup primarily primes system caches. Allocations cover the UI thread, not total process memory. These workloads do not measure frame latency, scrolling smoothness, GPU cost, or every control. A passing budget is evidence for these scenarios, not a guarantee against every performance regression. If the sample becomes incompatible with the old framework API, repair the comparison explicitly instead of silently dropping the baseline.
