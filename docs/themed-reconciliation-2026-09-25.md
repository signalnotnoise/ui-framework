# Themed reconciliation and application cleanup investigation

The previous three-sample comparison missed the themed update time budget
(+12.97% versus 12%). This investigation retains that failure and the original
accepted baseline and budgets.

## Rejected checkmark experiment

Changing the unchecked mark from Collapsed to Hidden avoided remeasuring it on a
toggle and passed five appearance/automation/layout checks. However, the identical
full-list, virtualized and themed workloads against immediate predecessor
`c39d4ae` did not justify keeping it. Themed update time improved only 0.46%, while
mount time rose 8.31%, mount allocations 0.39% and update allocations 0.48%.
Component work was unchanged. The change and its implementation-specific test
assertions were reverted, with [raw evidence retained](performance-evidence/2026-09-25-toggle-layout-rejected/summary.md).

## Trace and focused reconciliation fix

A separate managed stack-sampling trace of `c39d4ae` showed significant UI-thread
stacks under tree-change inheritance and layout/text measurement. The aggregate
profile includes background/finalizer waits and is not CPU utilization. The
per-thread analysis likewise measures sampled stack time, covers startup as well
as updates and contains overlapping inclusive stacks; do not add those totals.
The pinned `dotnet-trace` version was `10.0.745401`, using
`collect --profile dotnet-sampled-thread-time --format Speedscope` on the archived
pre-change Counter's `--compare --themed` workload. See the
[tool documentation](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace)
for the meaning of sampled thread time. The raw trace remains at
`artifacts/themed-profile-2026-09-25/current.nettrace`; its SHA-256, profile summaries
and analysis script are retained in
[trace evidence](performance-evidence/2026-09-25-themed-profile/trace-hash.json).

Source inspection found a concrete cause of unnecessary tree changes. Reordering
`A,B,C,D` to `B,C,D,A` removed and reinserted B, C and D. With 1,000 rows, this can
detach 999 unchanged subtrees instead of moving A once. The renderer now recognizes
an adjacent desired child and moves the displaced child forward to its target
position. Ordinary reconciliation remains the fallback; native identity, final
order and child layout are preserved. It does not suppress required property,
theme, input or accessibility changes.

The regression test inherits a marker into native controls. It fails on `c39d4ae`
because the adjacent retained control loses/regains the marker twice, and passes
with the fix without either change. It also checks reverse ordering, insertion,
removal and retained native identity/inherited values.

The three-sample alternating before/after campaign against `c39d4ae` used
`tools/Test-Performance.ps1`, one warmup per side/scenario, unchanged data and
operations, and identical explicitly experimental editor cleanup on both sides.
All 32 metric budgets passed:

| Scenario | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | -2.36% | -32.79% | -0.02% | -21.75% |
| Virtualized | +0.21% | +6.83% | approximately 0% | +0.99% |
| Themed full list | -0.16% | -64.55% | -0.08% | -59.63% |
| Editors, experimental cleanup | +0.99% | +3.75% | approximately 0% | +0.01% |

Themed update medians were 11,644.15/4,127.64 ms and
1,159,151,720/467,938,016 UI-thread bytes (before/after). Full-list medians were
5,933.38/3,987.63 ms and 604,987,024/473,376,672 bytes. Component work matched in
every scenario. Virtualized/editor time did not improve in this short comparison;
keep those increases visible rather than claiming universal improvement.
[Raw before/after evidence](performance-evidence/2026-09-25-panel-forward-move/summary.md)
includes the regression's pre-change failure and candidate pass. Final correctness
validation passed 98 framework tests, 15 visual checks and 21 stress assertions.

The original default-cleanup editor timeout remains a separate blocker. Neither
this experimental editor result nor the consumer's opt-in enables NuGet release.

## Seven-sample confirmation against the published baseline

The final comparison against unchanged accepted revision `78c88fb` completed all
64 processes (seven measured pairs plus a warmup pair per scenario) on SDK
10.0.401/runtime 10.0.12. It met all 32 existing metric budgets:

| Scenario | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +0.62% | -35.29% | -1.03% | -25.60% |
| Virtualized | +3.34% | -0.85% | -1.61% | -5.12% |
| Themed full list | -4.19% | -61.64% | -2.76% | -57.06% |
| Editors, experimental cleanup | -2.14% | -5.52% | +0.13% | -3.30% |

Themed update medians were 11,134.23/4,270.61 ms and
1,088,814,576/467,542,120 allocated bytes. Full-list update medians were
5,994.61/3,879.18 ms; virtualized 1,129.74/1,120.16 ms; experimental editors
8,528.02/8,057.29 ms. Component work matched in every scenario. Mount increases
remain visible in the table and are within the unchanged limits.

[Final raw evidence and source snapshots](performance-evidence/2026-09-25-panel-published-baseline/summary.md)
preserve all runs and ranges. This clears the measured themed slowdown without
loosening its budget. The declared experimental editor policy remains identical
on both sides, and the summary correctly reports `passed: true` with
`releaseEligible: false`. The ordinary cleanup/editor gate is still unresolved;
there is no NuGet release or baseline promotion in this change.

## Application cleanup integration

Lab-Feedback-WPF adds an explicitly default-off `--experimental-com-cleanup`
startup option. App owns installation and cleanup after MainWindow disposal;
failures persist in a count across recovery, are logged, and result in nonzero
exit. Its package pin stays `0.1.0-alpha.2-local.3`.

All 190 consumer tests passed. The actual-app layout comparison completed all 16
processes with behavior checks, using three alternating measured pairs plus a
warmup pair per clean/probe mode. Clean update time changed from 640.89 to 665.27
ms (+3.80%), allocation from 62,111,976 to 62,172,968 bytes (+0.10%), startup from
646.79 to 634.73 ms, and whole-process time including shutdown from 1,741.47 to
1,747.51 ms (+0.35%). Both modes realized 15 students and 19 files.

Instrumented policy startup was substantially slower (1,204.94 versus 657.73 ms);
instrumented update ranges overlap. Keep this evidence rather than treating the
native editor diagnostic's speedup as an app-wide benefit. The policy stays
experimental. [Consumer raw evidence](performance-evidence/2026-09-25-consumer-cleanup/summary.json)
includes exact startup/policy/harness snapshots. Programmatic undo, selection and
input-method-enabled checks passed; actual typing, IME composition and screen-reader
acceptance remain pending in the app's `COM_CLEANUP_EXPERIMENT.md` checklist.
