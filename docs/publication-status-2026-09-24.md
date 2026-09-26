# Source publication and package gate — September 24, 2026

> September 26 resolution: the package now exposes `WpfComCleanupPolicy`, the
> full seven-sample framework gate passes all 32 checks with
> `releaseEligible: true`, and the 15-sample consumer comparison passes all 28
> checks. This document retains the original blocked publication evidence.

Framework source was committed and pushed to `main` as `379cd58c76b9f06856a5777f6325f99acef3ddc6`. The consumer migration and file-tree work was pushed to `signalnotnoise/Lab-Feedback-WPF`, branch `GuidedGrade`, as `da092eef8bf84b4550ba4742a42aab57edb85918`.

Local framework Release validation passed 97 tests, 15 visual checks, 21 stress assertions, and documentation/graph validation, with zero build warnings or errors. The consumer passed 188 tests. [Hosted framework validation](https://github.com/signalnotnoise/ui-framework/actions/runs/36080344572) passed both correctness and the standard performance job.

## Additional local release gate

`tools/Test-Performance.ps1 -IncludeLayoutEditors -OutputDirectory artifacts/performance/publish-candidate-2026-09-24` ran against accepted baseline `78c88fb901c202e3c2e49b6de300d1ce2369e00b`, with the existing budgets, seven alternating measured pairs and one discarded warmup per side. Both sides used the same harness, machine and workload of 1,000 rows and 50 operations. All 110 candidate source-manifest hashes match the pushed framework source.

| Completed scenario | Mount time change | Update time change | Mount allocation change | Update allocation change |
| --- | ---: | ---: | ---: | ---: |
| Full list | +3.20% | +5.50% | -1.01% | -4.64% |
| Virtualized | -0.21% | +5.63% | -1.49% | -5.12% |
| Themed full list | +1.42% | +8.93% | -2.79% | +6.44% |

These three completed comparisons are within the existing budgets; positive timing changes are slowdowns, not improvements. Component body builds, mounts and unmounts were unchanged in every completed scenario. Timing varied substantially between processes; the raw ranges are retained. Allocations cover the UI thread.

The **baseline layout-editor warmup timed out after 120.031 seconds**, with 21.188 seconds of process CPU time. It produced no completed report. The candidate editor run was never reached. This is an incomplete release comparison, not proof of a candidate regression, and not a passing editor gate. No stack was captured from this specific process, so the earlier text-services investigation does not establish its cause.

The [partial summary](performance-evidence/2026-09-24-publish-candidate/partial-summary.json), [failure metadata](performance-evidence/2026-09-24-publish-candidate/failure.json), source manifest and all completed raw reports/logs are preserved. The partial summary explicitly marks the campaign incomplete and not passed; it does not substitute for the runner's missing complete summary.

## Release decision

No new NuGet release was dispatched. `0.1.0-alpha.2` remains the package version; the new changes remain Unreleased. Neither the baseline nor budgets were advanced. A successful editor comparison is still required before preparing the next immutable package version. The next investigation should capture the editor stall on a stable measurement session and establish its cause before retrying the same gate.
