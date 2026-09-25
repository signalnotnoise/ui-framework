# Layout isolation, September 24

Follow-up: [selectable submission-list virtualization](submission-virtualization-2026-09-24.md) measured an 88% update-time reduction against its equivalent nonvirtualized ListBox reference. This is a separate workload, not a revised result for the StackPanel comparison below.

This follow-up does not reproduce the earlier 39% workspace slowdown. It does not justify replacing Dock with a dedicated WPF panel yet. Framework runtime sources were unchanged; the new code is diagnostic tooling.

## Method

`Test-WorkspacePerformance.ps1 -Isolation dock|column|split|workspace` compares the native shell with one substituted primitive, or the complete framework composition. Each clean campaign uses seven alternating fresh-process pairs and one discarded warmup per side. All cases retain 1,000 submission rows and a 1,000-line editor for the same 50 resize/collapse operations. Every operation checks editor dimensions, position, selection and native ownership; disposal checks exactly two releases.

The isolated Dock and FlexColumn trees remain static while native split columns change. SplitPane and the full composition rebuild their bound host 50 times. These are controlled substitutions, not identical renderer-body workloads. Hybrid mount scaffolding also differs, so mount results describe these harness configurations rather than pure primitive construction costs.

Separate three-pair instrumented campaigns use decorators at the editor and submission-list boundaries to count and time downstream measure/arrange work. The same decorators exist with instrumentation disabled in clean campaigns. Timers do not count every internal panel pass, and instrumented elapsed times must not replace clean timings. The added decorators mean comparisons with previous campaigns are not direct before/after optimization evidence.

## Clean results

Values are native / framework medians. No baseline or budget changed. Each case links to raw reports, logs, source manifest and summary.

| Case | Mount ms | Update ms | Update change | Mount UI-thread bytes | Update UI-thread bytes | Mount bodies | Update bodies |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| [Dock](performance-evidence/2026-09-24-layout-isolation/dock-clean/summary.json) | 485.14 / 477.99 | 1,259.80 / 1,294.81 | +2.78% | 21,534,296 / 21,593,816 | 196,530,008 / 196,735,920 | 7 / 7 | 0 / 0 |
| [FlexColumn](performance-evidence/2026-09-24-layout-isolation/column-clean/summary.json) | 462.39 / 463.05 | 1,231.79 / 1,262.77 | +2.52% | 21,534,296 / 21,536,984 | 196,529,912 / 196,518,240 | 7 / 5 | 0 / 0 |
| [SplitPane](performance-evidence/2026-09-24-layout-isolation/split-clean/summary.json) | 442.00 / 452.79 | 1,212.93 / 1,241.14 | +2.33% | 21,534,296 / 21,573,440 | 196,530,008 / 196,754,352 | 7 / 8 | 0 / 50 |
| [Full composition](performance-evidence/2026-09-24-layout-isolation/workspace-clean/summary.json) | 443.29 / 452.49 | 1,227.82 / 1,217.62 | -0.83% | 21,534,296 / 21,550,048 | 196,530,008 / 197,221,320 | 7 / 1 | 0 / 50 |

Full-composition update ranges were 1,207.20–1,272.39 ms native and 1,194.03–1,260.26 ms framework. The small median advantage is not an established speedup. Update allocations remain 0.35% higher. Every mode creates the same two tracked native controls and releases neither during updates.

## Layout probes

All 24 measured instrumented processes, across all four cases and both sides, recorded the same update boundary counts: editor 100 measures / 100 arranges; submissions 34 measures / 34 arranges; zero unbounded measures at either boundary. The harness yields to dispatcher work and then applies its explicit viewport layout, so these counts are not one call per operation. They do not prove equal internal panel work, but show no extra downstream passes reaching either expensive native island.

Submission-list arrangement accounted for 77–81% of total update elapsed time in every instrumented process, including both native and framework modes. In the [full-composition probe campaign](performance-evidence/2026-09-24-layout-isolation/workspace-probes/summary.json), list-arrange medians were approximately 1,034 ms native and 996 ms framework. This includes WPF work below the list boundary; it is not a CPU profiler attribution to a particular row method.

## Decision

The isolated results do not establish a large nesting penalty. Keep the original 39% result and the unsuccessful column-reuse experiment as evidence of variability; this session does not retroactively turn them into passes. No speculative runtime rewrite was retained.

The next practical experiment is submission-list virtualization, retaining this full-list baseline and the same logical data and operations. It must also verify selection, scrolling, accessibility and disposal. This is a stronger measured lead for this workload than changing Dock. It does not establish the bottleneck in the complete consumer application. The separate text-services stall remains unresolved.

All eight campaigns completed: 56 clean measured processes, 24 instrumented measured processes, and 16 discarded warmups. Their geometry, selection and ownership assertions passed. Release builds had no warnings or errors. No package release or application migration was made. `Test-Performance.ps1` remains required before accepting a runtime optimization; these diagnostic campaigns do not replace the full-list, virtualized, themed or editor release checks.
