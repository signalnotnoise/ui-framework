# Paired performance measurement — September 18, 2026

Compared published revision `78c88fb901c202e3c2e49b6de300d1ce2369e00b` against the working changes based on `100ce9b`, using .NET 10.0.12 on Windows. Each side used the identical current sample, 1,000 logical rows, 50 deterministic updates, three measured fresh processes per scenario, alternating execution order, plus one discarded warmup per side. These are offscreen WPF workload measurements, not frame-rate claims.

The initial check caught a themed-update regression: **10.88% more UI-thread allocation and 11.08% more elapsed time**. The original failure is preserved. Optimizations now patch unchanged sibling order directly, skip unchanged layout-property writes, freeze shared checkmark geometry, and avoid measuring invisible marks. Compact toggle spacing keeps short labels on one line while retaining long-label wrapping, focus visuals, native input, and automation.

Final median changes against the published source (negative is improvement):

| Scenario | Mount time | Update time | Mount allocations | Update allocations |
| --- | ---: | ---: | ---: | ---: |
| Full list | −0.78% | −0.46% | −0.45% | −2.76% |
| Virtualized list | −6.22% | +0.81% | −0.01% | +0.13% |
| Themed full list | +0.43% | +9.68% | −3.39% | +5.63% |

Component builds and mount/unmount counts were unchanged in all scenarios. Themed allocations remain above the published version; this is not a claim of universal improvement. Timing varied across repetitions, and the themed update median is close to the 10% time budget. Keep measuring; do not interpret a pass as proof of equivalent latency on other machines.

The user explicitly accepted approximately 6% extra themed-update allocation for now. The budget file therefore records a scoped 6% exception for that metric only; other allocations remain limited to 2%, time to 10%, and component work to zero growth. Final measurements pass this approved policy. The run had already loaded the original 2% policy, so its original `summary.json` correctly remains a failure; `accepted-summary.json` reevaluates the same measurements under the approved exception without rerunning or changing data.

[Retained comparison evidence](performance-evidence/2026-09-18.json) includes the initial failure and final accepted assessment, medians, ranges, revisions, and environment details. Raw local samples and logs remain under `artifacts/performance/initial-guardrail`, `optimized-guardrail`, and `final-guardrail`; future CI runs upload their raw evidence. Intermediate one-run probes are exploratory and are not the basis of the final medians.

Validation: 48 framework tests passed; the renderer optimization also passed 15 visual checks and 21 assertions in the 200-operation full-list stress workload. Workflow syntax and knowledge-graph checks passed. The accessibility API was committed separately as `100ce9b`. No package was republished.
