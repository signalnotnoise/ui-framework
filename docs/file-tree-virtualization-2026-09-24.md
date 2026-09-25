# Consumer file-tree virtualization

File-tree recycling is implemented in Lab-Feedback-WPF. The framework runtime and pinned package are unchanged. The final integrated comparison improved median update time by 20.60% and reduced update UI-thread allocations by 24.49% in the 1,000-file workspace workload.

## Implementation and regressions fixed

`MainWindow.NativeControls.ConfigureFileTree` enables recycling and content scrolling and supplies VirtualizingStackPanel templates for root and nested items. A two-way IsSelected binding uses the existing FileSystemItem model alongside its existing expansion and analysis-checkbox bindings. Simply enabling recycling initially lost the selected-row highlight after the row returned to view; model-backed selection fixed that.

A subsequent actual-file check caught a second issue: selection restoration could invoke the file-open handler again and reset the active editor. `FileTreeView_SelectedItemChanged` now skips reopening the already-active path, preserving its document, caret and loaded review state. Explicit tab-switch behavior is unchanged.

## Final integrated measurements

Seven alternating fresh-process pairs compare the final app's normal virtualized tree with a forced full-tree reference. Both sides use the same final application code, including the selection/caret fixes; the reference switches off virtualization and uses StackPanel item panels. Both contain 1,000 logical students, 1,000 logical files and a 1,000-line editor, with 50 identical window/navigation-pane resizes. One warmup per side is discarded. Separate seven-pair instrumented runs corroborate attribution. The complete campaign comprises 32 processes.

[Final integrated raw evidence](performance-evidence/2026-09-24-file-tree-virtualization/integrated/summary.json) retains reports, logs and source hashes. Exact source snapshots are also retained locally under `artifacts/performance/consumer-tree-integrated-2026-09-24/source`.

| Clean median | Full tree | Virtualized | Change |
| --- | ---: | ---: | ---: |
| Startup/setup ms | 1,847.18 | 722.04 | -60.91% |
| Startup/setup UI-thread bytes | 113,812,896 | 14,851,112 | -86.95% |
| Update ms, 50 operations | 925.78 | 735.10 | -20.60% |
| Update UI-thread bytes | 82,221,896 | 62,088,376 | -24.49% |
| Realized file rows at initial viewport | 1,000 | 19 | same logical files |
| Realized student rows | 15 | 15 | unchanged |

Full-tree update range: 848.69–1,337.88 ms; virtualized: 618.62–797.99 ms. Startup includes app construction, synthetic data, diagnostic SQLite setup and initial layout, not just file-row creation. Component body counts are not instrumented here, and no fewer-builds claim is made. These results describe a large synthetic workspace, not every real project or frame-time distribution.

Instrumented file arrangement medians fell from 243.56 to 11.17 ms. File measurement increased from 4.75 to 42.93 ms, and the clean explicit-layout phase increased from 330.01 to 431.34 ms; total update elapsed time still fell as dispatcher-phase work decreased. The added virtualization bookkeeping is not free. Nested/root timings overlap and must not be summed. Keep these tradeoffs and raw repetitions with the aggregate improvement.

## Validation

The full consumer suite passed **188 tests**, including the new `RecyclingRetainsNestedStateAndOpenDocument` regression. Afterward, that regression was strengthened with 302 children in an expanded nested folder and passed again. It checks bounded realization, model-backed expansion, selected-file retention offscreen and on return, checkbox binding and absence of checked-state leakage, UI Automation selection/expansion patterns, real temporary-file opening, AvalonEdit document/caret retention, and host disposal. The integrated campaign runs the same behavior checks after timing, before the added large-nested-folder assertion. No manual screen-reader or keyboard-navigation certification is implied.

Release builds had zero warnings/errors. No app data, package pins or framework baseline/budgets were changed. No NuGet publication was performed. This is an app-native control optimization, not a framework runtime change; the unchanged framework Counter revision benchmarks were not rerun. The separate framework editor text-services diagnostic is still unresolved.

## Retained earlier attempts

The [earlier prototype comparison](performance-evidence/2026-09-24-file-tree-virtualization/prototype/summary.json) measured a 28.54% update reduction before the real-file caret check exposed redundant reopening. It is not the final result. Selection-only and checkbox-only checks were insufficient; the integrated result above includes the required active-file fix.

The first baseline automation check threw from WPF's selection provider before nested peers were initialized. Materializing the peer hierarchy resolved the harness issue. Recycling-only selection then failed with `selected=False, modelSelected=False, checked=True, otherChecked=False`; adding model-backed selection fixed it. The first real-file candidate failed document/caret retention; the active-path guard fixed it. These failures were observed during smoke validation and no failing candidate was left enabled in the app.

Reproduce using `tools/Test-ConsumerLayout.ps1 -ConsumerProject '<app>/Lab Feedback WPF/Lab Feedback WPF.csproj' -CompareFileVirtualization -Samples 7 -OutputDirectory <new-directory>`. The benchmark keeps the full-tree reference and the original window-versus-splitter diagnostic remains available without the comparison switch.
