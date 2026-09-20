# Native interop validation — September 20, 2026

**Follow-up status: the unchanged runtime passed a seven-sample paired repeat of every scenario under the existing budgets.** No runtime fix, baseline promotion, or budget increase was made. The original failures below remain part of the evidence. Local package `0.1.0-alpha.2-local.2` is unchanged; no new binary version or public release was produced.

The retained WPF host adds nine regression tests. All 57 framework tests pass, including native selection retention, keyed identity, type replacement, preservation of native Grid children, accessibility labels, parent/null rejection, setup/update failure cleanup, navigation recreation, removal/remounting, and sibling cleanup after a throwing release. Release validation also passed 15 visual checks and 21 full-list stress assertions. The final dispatcher adjustment was followed by another passing 57-test suite.

## Original measurements failed the release gate

Both comparisons used `tools/Test-Performance.ps1`, unchanged workloads and budgets, three measured processes per side/scenario, alternating order, and one discarded warmup per side. Full-list, virtualized, and themed full-list workloads were all retained.

An initial adapter implementation compared against immediate predecessor `74fbba6` failed themed update time (+14.09%), with themed update allocations +0.11%. The final implementation added complete initial-failure cleanup and restricted native-type checks to platform views. Its comparison against published baseline `78c88fb` produced:

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | **+14.95% — failed** | −2.83% | −0.26% | −2.64% |
| Virtualized | −1.35% | +7.84% | +0.28% | +0.92% |
| Themed full list | −4.37% | +2.78% | −3.36% | +5.79% |

All component build and mount/unmount counts matched their baseline. The full-list mount median exceeded the 10% time budget. Timing variation does not justify discarding a failure or declaring it harmless. Investigate startup/layout cost and repeat controlled measurements before publishing. The user's existing 6% exception applies only to themed-update allocations; no budget or baseline was changed for native interop.

[Retained evidence](performance-evidence/2026-09-20-native-interop.json) includes both comparisons and repetition ranges. Raw runs remain in `artifacts/performance/native-interop-before-after` and `artifacts/performance/native-interop-final`.

Local `0.1.0-alpha.2-local.2` packages are for application integration testing only. Their functional checks do not represent a performance pass or approval for public NuGet publication. The package verification script checks a fresh package-only restore, existing state/button behavior, and 1,000 retained native updates with exactly one creation/release. That single-run native diagnostic has no historical native-host baseline and is not a speedup claim.

## Seven-sample reproduction

The same runtime source (`d3e2001`) and sample workload were compared against the unchanged published reference. Seven measured fresh processes per side/scenario, plus discarded warmups, completed successfully. Every metric passed its existing budget; component builds and mount/unmount counts were unchanged.

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | −3.94% | −1.71% | −0.28% | −2.61% |
| Virtualized | +0.76% | −0.68% | +0.28% | +0.93% |
| Themed full list | −2.51% | +4.60% | −3.21% | +5.75% |

The original three-sample mount failure had broad ranges: baseline 5.16–6.64 seconds, candidate 5.29–6.27 seconds. Its 14.95% median slowdown did not reproduce in the larger run, whose full-list medians were 5.36 seconds and 5.15 seconds. This supports measurement/environment variability as an explanation, but no profiler established a specific cause. Do not describe this as a verified code optimization or a guarantee of identical latency. The migration app's test suite briefly overlapped the start of the repeat; the agent then reported heavy builds/tests complete. All samples were retained rather than selectively excluding that period.

The runner default is now seven samples for local, CI and release checks. Budgets, workloads and baseline are unchanged; failures still fail without automatic retries. Preserve both failures and this successful repeat when assessing future release readiness. The migration task separately reported all 170 app tests passing with the unchanged local packages.

[Repeat evidence](performance-evidence/2026-09-20-native-interop-repeat.json) contains the medians, ranges and environment. Raw samples are in `artifacts/performance/native-interop-reproduction`. The local feed contains a supplemental follow-up report; its original manifest/validation record and package hashes remain untouched.
