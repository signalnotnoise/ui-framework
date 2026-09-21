# Performance techniques — September 20, 2026

The largest measured win is the existing virtualization option. The retained source change reduces dependency-tracking allocations; it does not establish a general latency improvement. The published themed-allocation gate still fails.

This investigation evaluates several techniques and measures one implementation change without changing the workload, public API, accepted reference, or regression budgets. Measurements use `tools/Test-Performance.ps1`, Release builds, 1,000 logical rows, 50 deterministic mixed operations, alternating fresh processes, and a discarded warmup per side. Full-list, virtualized, and themed full-list scenarios remain present.

## Starting guardrail result

The seven-sample comparison of the starting revision against the published reference **failed**. Themed update time was 33,507.34 ms versus 30,361.71 ms (**+10.36%**, budget 10%); themed update allocations were 1,172.74 MB versus 1,087.73 MB (**+7.82%**, budget 6%). Other scenario metrics were within budget. Body-build and mount/unmount counts matched the reference in every scenario.

This failure predates the dependency-set experiment and confirms the allocation failure in the [earlier button-content validation](button-content-validation.md). It is retained rather than replaced by a later result. Today's absolute full-list timings were substantially slower than that historical report, so historical wall-clock values are not used as a control.

## Optimized result against the published reference

Three measured samples per side and scenario, plus discarded warmups, compared the optimized working tree against the same published revision. Negative percentages indicate improvement. These are total differences from the published source, including changes that predate this experiment; they do not isolate the new optimization's contribution.

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | +1.07% | -1.29% | -0.54% | -3.49% |
| Virtualized | -0.72% | +4.76% | -1.18% | -3.59% |
| Themed full list | -0.63% | +9.26% | -2.38% | **+7.19% — failed 6% budget** |

The performance gate still fails. Themed updates allocated 1,165,948,752 bytes versus 1,087,783,040 bytes. This is about 12.90 MB above the existing 6% allowance. The time result is within budget in this shorter run, but a pass does not establish equivalent latency; the starting seven-sample campaign failed that metric. No budget, baseline, workload, or control behavior was relaxed.

Within the optimized campaign, themed updates allocated about **1.92 times** as much as unthemed full-list updates (1,165.95 MB versus 608.48 MB), with identical component-work counts. This makes template/control allocation profiling a more targeted next step than assuming fewer component builds will solve the remaining cost.

Component work matched the reference in every scenario. Full and themed lists each had 1,022 mount body builds, 5,141 update body builds, 456 update mounts, and 658 update unmounts. Virtualized lists had 27, 194, 98, and 95 respectively.

## Isolated effect of the dependency-set change

A separate comparison against starting revision `bf519dcd2c51975705bf8f827e8981cc717a5eea` used three alternating measured samples per side/scenario and discarded warmups. Only the two dependency-tracking implementation files differ in runtime behavior. Both sides use the same renderer, sample, SDK, and operations. This isolates the experiment from older differences against the published reference.

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.16% | -0.77% | -0.30% | **-1.08%** |
| Virtualized | -0.73% | +0.85% | -1.34% | **-4.47%** |
| Themed full list | +1.84% | -0.19% | -0.32% | **-0.59%** |

Update allocations fell from 614.23 to 607.57 MB, 64.99 to 62.08 MB, and 1,172.63 to 1,165.65 MB respectively. All component-work counts were unchanged. Every metric is within its comparison budget against the starting revision, but that does **not** override the failed published-baseline gate above.

Timing ranges overlap in all three scenarios. For example, full-list updates ranged from 14,874.86–16,940.07 ms before and 15,924.61–16,838.41 ms after. Themed updates ranged from 33,908.40–34,708.06 ms before and 33,480.82–34,895.78 ms after. Three samples are exploratory; these results support a modest allocation improvement, not a general latency claim. The patch is retained because it removes redundant allocation without changing semantics or increasing component work.

## Techniques investigated

### Existing virtualization: largest application-level difference

Seven measured runs of the starting revision (`bf519dcd2c51975705bf8f827e8981cc717a5eea`) produced these medians. MB means decimal megabytes of UI-thread allocations.

| Scenario | Mount ms | 50 updates ms | Mount MB | Update MB | Mount body builds | Update body builds | Update mounts / unmounts |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Full list | 7,698.65 | 17,961.12 | 345.47 | 614.75 | 1,022 | 5,141 | 456 / 658 |
| Virtualized | 353.94 | 1,126.95 | 9.81 | 64.99 | 27 | 194 | 98 / 95 |

For this workload, virtualization reduced update time by about 93.7% and update allocations by 89.4%. Both scenarios have 1,000 logical rows, but they intentionally realize different numbers of native controls. This is evidence for the existing virtualization option, not a new source optimization or a scrolling-frame benchmark.

All runs retain the existing explicit Memo inputs and projected observation. Those techniques already suppress unrelated component rebuilds; their separate contribution was not remeasured here. Removing them or reducing the mixed-operation workload would invalidate this comparison.

### A: compare existing dependency sets directly

`ViewSession.Build` and general `Computed<T>` evaluations already hold dependency HashSets. Their `Except` calls construct temporary comparison sets and iterator machinery. Iterating each existing set and calling `Contains` on the other removes that duplication. The new-dependency subscription order, rollback on failed subscription, old-dependency removal, and ordinary-prop reevaluation remain intact. This is an allocation optimization, not a reduction in component work.

### Candidate: allocate key validation storage on demand (not implemented)

`ViewHost.Validate` allocates a HashSet for every descriptor, including leaves and containers with no keyed children. Creating it only on the first keyed child could avoid empty sets while keeping recursive validation and duplicate detection. This is a source-review hypothesis, not a measured improvement in this pass. A follow-up should test duplicates after unkeyed children and identical keys in separate parents; it must not skip validation or assume view descriptions are immutable.

### Deferred: reuse dependency capture storage

Reusing capture sets could remove further allocations. It also retains capacity between builds and requires careful treatment of nested captures, exceptions, and reentrancy. The smaller direct-comparison change avoids those lifetime complications. Pooling is a separate experiment, not part of the reported implementation.

Another narrower candidate is avoiding dependency-set allocation during an unobserved general computed read: the current implementation captures a set even though it only reconciles subscriptions while observed. The getter must still run, outer dependency tracking must remain isolated, and first-observer attachment must still capture dependencies. This is also unimplemented and unmeasured; it should be tested separately from caching getter results.

### Deferred: cached/lazy computed values and batching

General computed getters currently refresh ordinary non-observable properties on explicit reads. Unconditional caching breaks that contract, including an existing regression check. Opt-in observable-only computations and explicit batching could reduce repeated upstream work, but need separate tests for reads during batches, diamonds, dynamic dependencies, cycles, exceptions, and observer cleanup. No speedup is claimed for this unimplemented technique. See the [reactivity investigation](reactivity-performance-investigation.md).

### Deferred: large-shuffle reconciliation

Virtualized reconciliation uses `ObservableCollection.IndexOf` and moves within the collection. Arbitrary large permutations can require quadratic work. An index map alone would not remove the cost of collection moves or WPF notifications. The standard 50-operation workload moves the first row to the end; it does not execute the sample's random `Shuffle` operation. A dedicated permutation workload is needed before choosing an algorithm or claiming a shuffle improvement.

## Scope and limitations

Virtualization reduces native realization, while full-list rendering realizes all matching rows. Their results are useful as an application choice, not interchangeable workloads for evaluating a source optimization. Virtualized rows retain logical state and release offscreen observers.

Timing includes offscreen WPF layout and fresh-process JIT work. Model construction occurs before the mount measurement; this is not total application startup time. Allocations cover the UI thread, not retained heap size or total process memory. These measurements do not establish visible scrolling smoothness, frame latency, GPU performance, or large-shuffle scaling. Samples and source patches are retained under `artifacts/performance/`; the accepted reference remains `78c88fb901c202e3c2e49b6de300d1ce2369e00b`.

## Next measurement priorities

1. Keep virtualization as the first application-level option for large lists, while preserving the full-list workload as the framework baseline.
2. Prioritize themed allocation and operation-level profiling to distinguish broad compact-mode layout, filtering/remounting, edits, and structural changes. Any template optimization must retain rich button content, wrapping, automation, and keyboard behavior covered by existing checks. Keep the unchanged aggregate guardrail alongside the diagnostic harness.
3. Measure random shuffles and reversals at several sizes before changing reconciliation. Include retained input/selection and logical-state checks alongside timing.
4. Establish core-only getter counts, allocations, and timing before introducing observable-only lazy computed values. Keep ordinary-property reads compatible.

## Correctness and provenance

All **64** Release regression tests passed, including three new checks for failed body builds, failed computed evaluation, and failed subscription attachment. They verify preservation of old subscriptions, rollback of newly attached readers, successful recovery, and cleanup. Existing checks cover dynamic dependency branches, diamonds, ordinary-property refresh, memoization, virtualization, styling, and native interaction.

Environment: .NET SDK 10.0.401, runtime 10.0.12, Windows 10.0.26200, 20 logical processors, on the same host throughout. No benchmark processes ran concurrently, and test/build work was kept outside measured runs. The sample workload and benchmark runner were unchanged.

The initial run built the clean starting revision before experimental edits began. Its final `candidateDirty` field is true because source edits were pending by the end of sampling; the measured binaries were not rebuilt during that run. Before/after assembly hashes and the exact runtime patch are retained with the evidence. The first sandboxed build attempt failed while accessing NuGet configuration and produced no measurements; the authorized rerun is the recorded starting campaign.

The later review follow-up factors the reconciliation helper, lazily allocates rollback storage, and skips unobserved computed capture sets. A three-sample paired run was subsequently completed at `artifacts/performance/review-fixes-performance-2`. Against the published reference, full-list update time changed by **-4.19%** and allocations by **-3.51%**; virtualized update time changed by **-1.75%** and allocations by **-3.53%**. Themed updates changed by **+11.72%** in time and **+7.29%** in allocations, so the guardrail still fails there. Component-work counts remained unchanged. Three samples are exploratory; use the normal seven-sample run for release decisions.

[Retained evidence](performance-evidence/2026-09-20-exploration.json) contains all three summaries, all 96 raw measured/warmup process reports, source provenance, and the runtime patch. Raw logs, archived reference source, and test results remain locally under:

- `artifacts/performance/exploration-before-authorized`: starting revision versus published reference; seven samples per side/scenario.
- `artifacts/performance/exploration-direct-membership`: optimized code versus published reference; three samples per side/scenario.
- `artifacts/performance/exploration-against-start`: optimized code versus starting revision; three samples per side/scenario.
- `artifacts/performance/exploration-correctness/checks.trx`: 64 passing tests.

Reproduce the optimized comparisons with new output directories so earlier evidence remains intact:

```powershell
./tools/Test-Performance.ps1 -Samples 3 -ReportOnly
./tools/Test-Performance.ps1 -BaselineRef bf519dcd2c51975705bf8f827e8981cc717a5eea -Samples 3 -ReportOnly
dotnet test 'tests/UI Framework.Checks/UI Framework.Checks.csproj' -c Release
```

`-ReportOnly` preserves failed assessments while allowing exploratory collection to finish; it must not be used to bypass CI. The normal guardrail still defaults to seven samples. No baseline promotion or budget change is proposed.
