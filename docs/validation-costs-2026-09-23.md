# Validation and notification cost investigation — September 23, 2026

The hardening changes were committed and pushed as `d5a94b2`. This investigation tests the hypothesis that added validation or notification delivery explains the previously observed 45.66 ms increase in virtualized update time against `f381269`.

## Measurement method

An isolated archive of `d5a94b2` instruments the outermost view-validation call and notification-delivery call with Stopwatch timestamps. Recursive validation is counted once. Nested notifications are counted individually, but their elapsed time is included only in the outer delivery measurement, avoiding double counting. Notification time includes subscriber application work, so it is an upper bound on delivery overhead, not a measurement of the dispatcher alone. The initial caller's thread-affinity check before entering delivery is outside that interval. Validation and notification measurements must not be added together in applications where synchronous subscribers render. Instrumentation changes call shapes and can affect JIT behavior, so the absolute diagnostic costs are estimates rather than a causal decomposition of the earlier uninstrumented run.

The unchanged Counter comparison still performs 1,000 rows and 50 mixed operations, with both full-list and virtualized scenarios. Mount and update counters are separated. Diagnostic instrumentation is present only in archived copies under `artifacts/performance/cost-investigation`; no instrumentation or validation-disable switch is shipped in the runtime. An initial seven-sample diagnostic is followed by seven alternating measured processes per side and one discarded warmup per side for the optimization comparison. Both sides have the same instrumentation and sample sources.

## Focused change

View validation uses explicit enum patterns instead of five generic enum metadata checks per description. The patterns enumerate the same accepted values. All dimension, metadata, child structure, duplicate key, and recursive validation remains in place; mutable child collections are not cached or skipped. Notification delivery is unchanged.

Regression coverage renders every declared value of each affected enum and rejects negative, extreme, and immediately out-of-range values before changing the retained tree. Enumerations added in future must remain covered by the validator and these checks.

## Evidence

Seven-sample instrumented medians (milliseconds across the complete mount or 50-update phase):

| Scenario | Validation phase | Before | After | Change |
| --- | --- | ---: | ---: | ---: |
| Virtualized | Mount | 2.0233 | 1.4914 | -26.29% |
| Virtualized | Updates | 10.0697 | 7.0595 | -29.89% |
| Full list | Mount | 7.3574 | 5.7835 | -21.39% |
| Full list | Updates | 3.1463 | 2.3001 | -26.89% |

Virtualized update validation improved in six of seven paired samples, with before values ranging from 6.2634 to 10.5078 ms and after values from 6.9813 to 8.5216 ms. The smaller full-list update validation time despite more component builds can depend on JIT/tiering and call shapes; these measurements do not isolate that difference. Timings from different scenarios must not be treated as a per-view cost model.

Inclusive notification delivery during virtualized updates measured 0.3861 ms before and 0.3930 ms after, across 70 deliveries. Full-list delivery measured 4.5402 and 4.5774 ms across 89 deliveries, including subscriber work. This does not justify changing notification exception isolation, thread checks, or nested notification semantics for this workload.

Even the entire measured pre-optimization virtualized validation cost is only about 10 ms, far below the earlier 45.66 ms slowdown. Notification delivery contributes less than 0.4 ms including callbacks. The initial explanation that these costs account for the slowdown was too strong. This change saves a measured 3.01 ms of validation, not an established 45.66 ms end-to-end regression. The instrumented virtualized total update median was actually 0.95% slower despite the reduced validation time, illustrating the need for an independent uninstrumented comparison and restraint about small aggregate changes.

Raw instrumented reports, source copies, and reproduction scripts are retained in `artifacts/performance/cost-investigation`. Instrumented timings diagnose cost and are not the performance acceptance gate. The ordinary `tools/Test-Performance.ps1` campaign compares the uninstrumented working tree with `d5a94b2`, retaining all three standard scenarios and the existing budgets.

The original 4.03% virtualized update regression remains a historical observation. Reducing one measured contributor does not establish the cause of the entire earlier difference, and these measurements do not measure frame or input latency.

### Uninstrumented campaign 1

`tools/Test-Performance.ps1 -BaselineRef d5a94b2 -Samples 7 -OutputDirectory artifacts/performance/validation-optimized-previous` passed all unchanged budgets. Before and after use SDK 10.0.401, runtime 10.0.12, the same machine and harness, and alternating fresh processes with discarded warmups.

| Scenario | Mount ms before / after | Update ms before / after | Update change | Mount bytes before / after | Update bytes before / after |
| --- | ---: | ---: | ---: | ---: | ---: |
| Full list | 5,692.83 / 5,471.53 | 6,699.55 / 6,960.53 | +3.90% | 342,687,808 / 342,647,280 | 603,567,856 / 603,512,160 |
| Virtualized | 392.38 / 384.75 | 1,170.16 / 1,144.00 | -2.24% | 9,593,992 / 9,591,616 | 60,598,528 / 60,591,776 |
| Themed full list | 5,307.98 / 5,372.77 | 13,145.83 / 12,963.93 | -1.38% | 327,921,888 / 327,902,720 | 1,157,865,376 / 1,157,960,968 |

Virtualized updates were faster in all seven pairs; full-list updates were slower in five of seven pairs. The full-list regression is reported even though it passes the budget. Instrumented full-list totals had moved in the opposite direction, so an independent confirmation campaign was started with unchanged source, workload, and budgets. No result is discarded in favor of a passing retry.

Component work is unchanged: full/themed mount builds 1,022, update builds 5,141, update mounts 456, update unmounts 658; virtualized counts are 27, 194, 98, and 95 respectively. There is no reduced workload or lost rendering work behind the validation optimization.

### Confirmation timing limitation

An independent, unchanged seven-sample campaign is retained in `artifacts/performance/validation-optimized-confirmation`. Its full-list update times shifted abruptly from roughly 15–16 seconds to 6–7 seconds for both revisions during the campaign. Sample 4 straddled that change: baseline 15,090.3 ms, candidate 6,265.5 ms. No source changes, tests, or concurrent benchmark campaigns occurred. The cause of this timing shift is not established.

Consequently, the confirmation's ratio of independent full-list medians is not credible evidence of a code speedup. The raw results must remain available, and its absolute medians must not be pooled with campaign 1. Paired changes offer additional context but cannot establish a clean causal result under these conditions. The first campaign's full-list slowdown remains disclosed.

The campaign completed and passed the unchanged budget checks. Its recorded medians are below for completeness; the full-list timing row is **confounded**, not a 56% optimization result.

| Scenario | Mount ms before / after | Update ms before / after | Update change | Mount bytes before / after | Update bytes before / after |
| --- | ---: | ---: | ---: | ---: | ---: |
| Full list (confounded timing) | 7,496.70 / 5,535.16 | 15,090.27 / 6,592.38 | -56.31% | 342,677,712 / 342,438,640 | 598,938,288 / 602,533,088 |
| Virtualized | 383.64 / 381.69 | 1,138.29 / 1,149.48 | +0.98% | 9,594,008 / 9,591,616 | 60,599,656 / 60,593,240 |
| Themed full list | 5,052.17 / 5,092.31 | 12,567.43 / 12,698.16 | +1.04% | 327,952,632 / 327,819,024 | 1,158,036,904 / 1,157,927,608 |

Component work remained identical to campaign 1. Virtualized and themed update timing directions reversed between campaigns, reinforcing that an overall speedup is not established. Both campaigns' 97-file candidate source manifests matched the final working sources with zero mismatches. The audit, including individual paired percentage changes, is retained in `artifacts/performance/cost-investigation/final-audit.json`.

The retained change has a measured benefit inside validation and passes correctness checks. End-to-end latency improvement and the cause of the original 4.03% slowdown remain unconfirmed. This investigation does not justify weakening validation or notification guarantees.

## Correctness

`tools/Test-Release.ps1` passed after the optimization: Release build with zero warnings/errors, all 89 tests, 15 visual checks, 21 stress assertions, and documentation/knowledge-graph checks. The existing renderer failure, malformed-description, notification exception, ownership, and focus checks continue to pass. No correctness suite ran concurrently with the performance campaigns. The accepted baseline revision and performance budgets are unchanged.
