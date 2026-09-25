# Actual consumer layout diagnostic

Follow-up: [file-tree virtualization is now implemented and measured](file-tree-virtualization-2026-09-24.md), with selection and active-editor regressions fixed. The measurements below describe the earlier nonvirtualized app configuration.

The actual Lab-Feedback-WPF workspace already virtualizes students. The file tree does not, and is the strongest measured lead for navigation-pane resize cost. This investigation changed diagnostic code only; no consumer source, package pin or framework runtime was changed.

## What was exercised

`samples/ConsumerDiagnostics` builds against the consumer project and its pinned `SignalNotNoise.UI.Wpf 0.1.0-alpha.2-local.3` package. It constructs the real MainWindow with separate diagnostic persistence databases, actual native controls, templates, framework shell and AvalonEdit editor. Synthetic data contains 1,000 students, 1,000 flat file entries and 1,000 editor lines. Student folders are null and files are not opened, so grading, disk traversal, builds and model requests are not invoked. Existing user grading databases are not used.

The content tree is hosted in a hidden HwndSource with transparent probes at the root, students, files and editor. This is the actual constructed consumer UI with synthetic data, not inspection of the user's running window or a manual interaction/frame-rate test. Existing panel preferences are loaded by the app constructor. The empty-state overlay is hidden for the synthetic editor.

Two scenarios alternate fresh-process order: 50 viewport changes between 1440x960 and 1200x800, and those same changes plus navigation-column widths varying from 260 to 290 pixels. The latter simulates layout changes from dragging the splitter; it does not synthesize mouse input. Both verify finite visible pane geometry and retained student/editor selection. A final scroll to student 999 verifies realization without losing selection. Three measured processes per scenario run with timers disabled, followed by three instrumented processes per scenario. Each mode/scenario has a discarded warmup: 16 completed processes total.

## Results

[Raw reports and summary](performance-evidence/2026-09-24-consumer-layout/summary.json), source manifest and binary provenance are retained. Full source snapshots remain under `artifacts/performance/consumer-layout-paired-v2-2026-09-24/source`. Consumer and diagnostic source hashes were checked again after the campaign and had not changed.

| Clean median | Window resize | Window + navigation resize |
| --- | ---: | ---: |
| Startup/setup ms | 1,967.42 | 1,858.16 |
| Startup/setup UI-thread bytes | 111,480,968 | 111,480,760 |
| Updates, 50 operations, ms | 492.10 | 988.74 |
| Update UI-thread bytes | 35,193,448 | 82,221,896 |
| Update range, ms | 485.57–496.76 | 978.36–1,010.00 |
| Realized students / logical students | 15 / 1,000 | 15 / 1,000 |
| Realized files / logical files | 1,000 / 1,000 | 1,000 / 1,000 |

Startup includes construction, synthetic data creation, schema initialization and initial layout; it is not an isolated framework mount measurement. Component-body counts are not instrumented in this consumer diagnostic. No before/after optimization or release-performance claim is made.

The student ListBox has virtualization enabled, a VirtualizingStackPanel, content scrolling enabled, and Standard virtualization mode. Scrolling to the last student leaves 29 containers realized and preserves selected student 7. Its accessible name remains Students. This confirms that the previous selectable-list benchmark's 88% virtualization benefit is not automatically available as a new app improvement.

The file TreeView has virtualization disabled, no VirtualizingStackPanel found beneath it, and all 1,000 top-level item containers realized. This is runtime evidence for the actual configured control, not an inference from WPF defaults.

Separate instrumented medians, across all 50 operations:

| Region | Window measure / arrange ms | Navigation-resize measure / arrange ms |
| --- | ---: | ---: |
| Students | 7.53 / 4.63 | 14.49 / 13.00 |
| File tree | 2.67 / 2.68 | 4.76 / 259.61 |
| AvalonEdit | 27.47 / 69.80 | 30.35 / 69.53 |

Each region receives 50 measure/arrange calls in the window case and 100 in the navigation case; changing a column before yielding schedules additional layout. The tree's arrangement is the largest of these three region timings during navigation resizing. It does not explain all elapsed time: dispatcher phases include other queued work and waits, and root timers can overlap child timers. Do not add those nested measurements together or attribute all dispatcher time to the tree. The instrumented navigation range includes a 1,633 ms outlier, retained in raw evidence.

## Next change to test

Test recycling virtualization on the file tree, retaining the full tree as a baseline. Because these synthetic files are flat, correctness coverage must add nested directories, expansion/collapse, selected-file retention, analysis-checkbox bindings, keyboard navigation and automation. The experiment identifies a target; it does not yet establish that enabling virtualization preserves all app behavior or delivers a particular speedup.

No packages were published. The framework's accepted baseline, budgets and unresolved text-services diagnostic are unchanged. Runtime changes still require `Test-Performance.ps1` and the consumer comparison; this diagnostic does not substitute for either release clearance or actual-file workflow testing.

## Reproduction and retained failures

Run `pwsh -File tools/Test-ConsumerLayout.ps1 -ConsumerProject '<app>/Lab Feedback WPF/Lab Feedback WPF.csproj' -OutputDirectory <new-directory>`. The project is intentionally not in the framework solution because it requires the external consumer checkout. Builds produce consumer bin/obj outputs but do not edit consumer source.

An initial lookup attempted to find the outer pane grid before WPF templates were materialized; that setup failed before timing and its log is retained. Moving the lookup after initial layout resolved it. The complete campaign's original XML package-version lookup then failed only while writing its summary; all 16 process reports were already complete. The lookup was corrected, and the summary was reconstructed from existing reports without rerunning measurements. An earlier exploratory report without the root/phase probes is retained separately and excluded from repeated medians.
