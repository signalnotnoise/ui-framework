# Submission-list virtualization experiment

Follow-up: [actual consumer diagnostics](consumer-layout-2026-09-24.md) confirm that students are already virtualized and identify file-tree arrangement during navigation resizing as a stronger app-specific target.

Recycling virtualization removes most update cost in the controlled selectable-list workload. Seven alternating fresh-process pairs, plus one discarded warmup per side, compared the same framework workspace with WPF ListBox virtualization disabled and enabled. Framework runtime source was unchanged.

## Equal workload

Both modes contain all 1,000 logical submissions, the same item template and selection, the same 1,000-line editor, and the same 50 resize/collapse operations. The full-list reference realizes all 1,000 rows. Virtualization realizes 34 rows at the measured viewport. Both use the same ListBox and VirtualizingStackPanel machinery; the IsVirtualizing flag is the difference. Recycling mode and cache settings are identical on both sides, though inactive when virtualization is disabled.

This is a new selectable-list comparison. The original nonselectable StackPanel workload remains intact as the runner's default. Its earlier timings are not the denominator for the speedup below. The experiment uses a native ListBox through WpfUI.Native; it is not a measurement of the framework's declarative VirtualList control.

## Clean measurements

[Raw reports, logs, source manifest and summary](performance-evidence/2026-09-24-submission-virtualization/clean/summary.json) preserve all runs. Timings are medians on the same machine and SDK, with alternating process order.

| Metric | Full list | Virtualized list | Change |
| --- | ---: | ---: | ---: |
| Mount ms | 761.58 | 578.71 | -24.01% |
| Update ms, 50 operations | 2,385.71 | 281.90 | -88.18% |
| Mount UI-thread allocated bytes | 42,575,784 | 11,774,664 | -72.34% |
| Update UI-thread allocated bytes | 233,490,920 | 12,343,912 | -94.71% |
| Mount / update body calls | 1 / 50 | 1 / 50 | unchanged |
| Tracked native creates / releases during updates | 2 / 0 | 2 / 0 | unchanged |
| Realized rows at mount / update end | 1,000 / 1,000 | 34 / 34 | fewer containers, same logical data |

Update ranges were 2,068.98–2,747.62 ms full and 230.50–400.29 ms virtualized. The ranges do not overlap; this gain is substantially larger than the prior small primitive-isolation differences. Mount ranges were 709.85–867.87 ms and 465.41–634.12 ms respectively.

## Behavior and diagnostic checks

All 16 clean processes and eight separate instrumented processes passed editor geometry, position, selection and native ownership checks. Submission selection remains item 7 throughout all timed resize/collapse updates. After timing, each process selects and scrolls to items 500, 999, 0 and 7, checks that the selected container represents the correct item and is inside the viewport, checks the accessible list name and single-selection provider, and checks realization stays below 200 containers when virtualized versus exactly 1,000 in the full list. The two adapter-owned native elements are each released exactly once at host disposal.

These are programmatic scrolling/selection and UI Automation provider checks, not a manual keyboard or screen-reader audit. WPF owns row-container recycling; the native ownership counters describe the two adapter islands, not every ListBoxItem allocation or garbage collection. Post-timing scroll checks are not included in the resize/collapse latency claim.

The [separate three-pair probe campaign](performance-evidence/2026-09-24-submission-virtualization/probes/summary.json) corroborates reduced downstream work. In the first measured pair, the list received 34 measure/arrange calls in both modes; list arrangement took 1,318 ms full versus 52 ms virtualized. The editor received 100 measure/arrange calls on both sides. Probe time is diagnostic and remains separate from the clean results.

## Application implication and limits

Read-only inspection of Lab-Feedback-WPF found that MainWindow.NativeControls.cs already constructs a native ListBox for students, and MainWindow.Framework.cs mounts it below a header in a finite grid region. No explicit virtualization-disabling setting was found in those files or the reviewed presentation styles. This means the application may already obtain WPF's default virtualization benefit. Its actual realized-container count and effective template need runtime verification before claiming an application gain. No app files or package pins were changed.

The experiment validates keeping selectable native lists virtualized and avoiding accidental devirtualization. It does not clear the outstanding editor text-services diagnostic, replace Test-Performance.ps1, change the accepted baseline/budgets, or authorize a public package release. No framework runtime optimization was made, so the unchanged Counter revision benchmarks were not rerun for these diagnostic-only additions.

Reproduce with `pwsh -File tools/Test-WorkspacePerformance.ps1 -CompareSubmissionVirtualization -OutputDirectory <new-directory>`. Add `-ProbeLayout -Samples 3` for a separate diagnostic campaign.
