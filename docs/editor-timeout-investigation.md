# Editor benchmark timeout investigation — September 24, 2026

> September 26 resolution: `WpfComCleanupPolicy` moves COM-wrapper cleanup to
> application-owned idle boundaries. The unchanged 1,000-editor diagnostic
> reduces final updates from 12.73-13.38 seconds to 0.19-0.22 seconds, and the
> seven-sample release gate passes. The original diagnosis below is retained.

Follow-up: [primitive isolation and layout probes](layout-isolation-2026-09-24.md) did not reproduce the large adapter gap and identified submission-list arrangement as the dominant cost in the representative workspace. The original evidence below remains unchanged.

The September 23 layout-editor timeout is not established as a regression from the workspace primitives. The pre-layout revision and candidate both pause inside WPF Text Services during programmatic text replacement. A standalone WPF program with no framework project or package references also reproduces a long wait in the same native text-services call path.

This identifies the blocked subsystem, not the underlying Windows/WPF fault or a proven recovery procedure. It does not clear the performance gate.

## Evidence

The candidate and pre-layout reference `2b678989d70158ffb40d31bfb949e18969439717` were built with identical optional tracing. Both completed updates 1–9 quickly and paused at update 10. Managed stack captures show `TextBox.OnTextPropertyChanged` → text selection notification → `TextStore.RequestLock` / `GrantLockWorker` → native waiting. The candidate capture includes `TextServicesDisplayAttributePropertyRanges.OnEndEdit`. CPU usage remained low while the calls waited. These exploratory processes were stopped after obtaining stack evidence; they are not completed timing samples.

Changing the offscreen source to include `WS_VISIBLE` did not avoid the pause at update 10. That diagnostic variant was removed; the benchmark's window configuration is unchanged.

The native reproduction creates 1,000 WPF TextBoxes, changes text every five steps, alternates read-only/undo settings, and changes layout width over 50 updates. It uses a UniformGrid instead of the framework's AdaptiveGrid, so its timings are diagnostic only, not an equivalent performance comparison.

- Native run: completed in 35,566 ms; update 50 alone took about 20 seconds.
- Native run with the renderer's caret-preservation calls: completed in 30,928 ms; update 50 took about 19 seconds. A stack captured during this pause shows the same `TextServicesDisplayAttributePropertyRanges.OnEndEdit` / `TextStore` wait, called directly from `TextBox.set_Text`, with no framework renderer in the stack.

Raw logs, stack captures and the standalone source are in [the evidence directory](performance-evidence/2026-09-24-editor-diagnosis/native-caret-stalled-stack.txt). The reproduction can be run with:

```powershell
dotnet run --project docs/performance-evidence/2026-09-24-editor-diagnosis/native-repro/NativeRepro.csproj -c Release
```

The native reproduction keeps input methods enabled. It is not an IME, screen-reader or live keyboard correctness test.

## Diagnostic changes

`LayoutEditorComparison` now accepts `--trace-layout`, which prints each operation's progress and separates dispatcher wait time from explicit measure/arrange work. A dispatcher wait includes the queued framework refresh; it does **not** by itself establish dispatcher starvation. The stack captures were necessary to identify the text-services call.

`tools/Test-Performance.ps1` now preserves a `failure.json` on timeout, including side, scenario, sample, CPU time and elapsed time. The timeout remains 120 seconds, and the 1,000-row/50-operation workload, budgets and accepted baseline remain unchanged. Diagnostic tracing is off during paired measurements.

A fresh seven-sample alternating campaign is retained in `artifacts/performance/workspace-layout-2026-09-24`. It failed at themed-full-list sample 1 on the **pre-layout baseline**, before reaching the editor scenario. The process wrote its report (23,726.65 ms mount and 88,559.53 ms updates), but had not exited at 120 seconds. Its recorded process CPU time was 126.95 seconds across threads; this is not the low-CPU wait seen in the editor trace, and no stack was captured from this themed process. The cause of this second failure is unconfirmed. [Failure and partial results](performance-evidence/2026-09-24-layout-retry/partial-summary.json) are retained along with every completed report; the incomplete themed sample is not a successful benchmark.

| Completed scenario | Mount ms before / after | Update ms before / after | Mount bytes before / after | Update bytes before / after |
| --- | ---: | ---: | ---: | ---: |
| Full list | 5,501.92 / 6,032.88 | 6,931.12 / 7,512.49 | 342,729,168 / 342,890,552 | 602,715,992 / 604,598,456 |
| Virtualized | 452.85 / 440.77 | 1,285.55 / 1,333.74 | 9,591,888 / 9,620,168 | 60,590,600 / 61,102,728 |

Full-list median updates were 8.39% slower, although the candidate was faster in four of seven individual pairs. Virtualized updates were 3.75% slower. Update allocations rose 0.31% and 0.85% respectively. Component work was unchanged (full: 1,022 mount builds, 5,141 update builds, 456 mounts and 658 unmounts; virtualized: 27, 194, 98 and 95). These completed scenarios are within existing budgets, but the overall campaign is incomplete. Previous failures remain part of the evidence; an eventual passing retry alone would not explain them.

## Integration constraints found during inspection

The app's native shell includes a vertical split with a star-sized first row capped at 300 pixels, a separately sized/collapsible right pane, editor overlays, and a collapsible bottom tools region. The first `SplitPane` API sizes and collapses its **first** pane only. Replacing every native Grid mechanically would change behavior. A migration should begin with equivalent header/body fill and fixed-edge docking, retaining adapters for unsupported sizing/overlay behavior. Actual populated-workspace measurements and visual/focus checks remain required before adopting a package.

## Representative adapter comparison

`samples/WorkspaceComparison` compares the supported header/body fill, fixed edge docking, first-pane sizing and collapse against the app's native DockPanel/Grid adapter pattern. Both modes retain 1,000 nonvirtualized submission rows and a 1,000-line editor for 50 identical operations. Every operation checks editor dimensions and selection; native factories must run exactly twice and native controls must remain mounted until disposal. Text replacement is deliberately a separate workload and remains blocked as described above.

The initial seven-pair campaign, including a discarded warmup per side, is retained in [adapter evidence](performance-evidence/2026-09-24-workspace-adapters/summary.json). Native/primitives median mount: 715.18/699.16 ms; updates: 2,138.68/2,974.13 ms (**39.06% slower**). Mount allocations: 21,523,288/21,543,696 bytes; updates: 196,498,680/197,189,992 bytes (+0.35%). The primitives were slower in six of seven update pairs. Mount body calls were 7/1, update body calls 0/50; fewer initial hosts did not prove lower update latency. Both modes created two native controls and released neither during updates.

Inspection found that FlexRow clears and recreates every column definition during each refresh, even when the number and sizing of columns are unchanged. A focused candidate retained definitions, adjusted their count as needed, and updated changed widths. The new regression check exercises width/gap changes, keyed editor reorder and selection, child removal and an empty row. **The optimization was subsequently reverted after the direct comparison below did not establish a speed benefit.** The regression check and benchmark tooling remain.

The second [adapter campaign](performance-evidence/2026-09-24-workspace-column-reuse/summary.json), with retained definitions, measured native/primitives mount medians of 655.53/628.28 ms and update medians of 2,038.25/2,321.18 ms (+13.88%). Mount allocations were 21,523,288/21,543,576 bytes; update allocations 196,498,736/197,079,288 bytes (+0.30%). Body and native-control work remained the same as the first campaign. The candidate allocation median fell by 110,704 bytes, but timing ranges overlap broadly and both sides shifted. The change from +39% to +14% is not proof of a 22% optimization. A direct source-revision comparison is needed.

The pre-optimization source snapshot is `c25c521f243cfa646223694486cb46b98e31cf2f`, created using a separate temporary Git index; the user's index and branch were unchanged. A focused three-pair run of `Test-Performance.ps1` against this snapshot completed within unchanged budgets for all three standard workloads. Full-list update median grew 8.22%, virtualized grew 1.09%, and themed decreased 0.58%; update allocations changed +0.12%, +0.01%, and -0.40% respectively. Body/mount/unmount work was identical. [All mount/update metrics and raw samples](performance-evidence/2026-09-24-column-reuse/summary.md) are preserved. These standard workloads do not directly exercise the FlexRow optimization, so they are guardrails, not evidence of its benefit. This focused run neither retries nor waives the failed editor diagnostic, and is not release approval.

### Direct optimization comparison and final decision

Seven alternating pairs compared the archived pre-optimization primitive implementation directly with column reuse, using the identical current workspace harness on both sides. The harness also verified editor position in this run, in addition to dimensions, selection and native ownership. [Raw evidence and summary](performance-evidence/2026-09-24-workspace-column-reuse-paired/summary.json) are retained.

| Metric | Before | Column reuse | Change |
| --- | ---: | ---: | ---: |
| Mount ms | 544.89 | 578.47 | +6.16% |
| Update ms, 50 operations | 1,802.66 | 1,894.15 | +5.08% |
| Mount UI-thread bytes | 21,544,336 | 21,543,576 | -760 bytes |
| Update UI-thread bytes | 197,189,992 | 197,079,192 | -110,800 bytes (-0.06%) |
| Mount / update body calls | 1 / 50 | 1 / 50 | unchanged |
| Native creates / releases during updates | 2 / 0 | 2 / 0 | unchanged |

Timing was noisy (before update range 1,652.18–9,395.75 ms; candidate 1,690.45–3,811.77 ms). The small allocation saving does not establish a latency improvement. The optimization was reverted, and an [audit of all 44 core/WPF source and project files](performance-evidence/2026-09-24-workspace-column-reuse-paired/revert-audit.json) confirmed that the runtime matches the pre-optimization snapshot after normalizing line endings.

Final validation passed 97 tests, 15 visual checks and 21 stress assertions, plus the documentation and knowledge-graph checks; the Release build had zero warnings/errors. The benchmark, optional tracing, timeout evidence, standalone reproduction and additional layout regression check are retained. The app and packages are unchanged. The original adapter performance concern and editor diagnostic remain unresolved; next work needs a more stable measurement session and investigation of the complete Dock/FlexColumn/SplitPane composition rather than assuming column allocation is the bottleneck.
