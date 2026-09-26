# Application-owned WPF COM cleanup

`UI Framework.Wpf/WpfComCleanupPolicy.cs` is the production WPF lifecycle API
for applications with substantial native text editing. Create exactly one owner
on the application STA before constructing WPF controls. Dispose windows,
`ViewHost` instances, and native owners first; then call
`CloseBeforeDispatcherShutdown` before the dispatcher exits.

Construction disables CLR eager COM-wrapper cleanup for that STA. The setting is
thread-wide and cannot be reversed, so ownership remains explicit rather than
being installed from `ViewHost`. Dispatcher operation completion coalesces
cleanup requests at `ContextIdle`. `RunUpdate` marks application update
boundaries so cleanup cannot enter a nested message loop while an update is
active. Cleanup failures remain observable through `LastError` and
`FailureCount`; callers can provide a nonmodal reporting callback.

## Root cause and diagnostic result

The ordinary 1,000-TextBox reproduction spends its late updates inside
`TextBox.Text` and WPF Text Services, not explicit layout or dispatcher waiting.
Three fresh runtime-default runs took 19.40-21.21 seconds; their final updates
took 12.73-13.38 seconds. The same workload using the packaged policy took
7.37-7.67 seconds, with final updates of 0.19-0.22 seconds. Input methods,
read-only/undo changes, 10,000 checked text writes, and the 50-operation workload
were retained.

Evidence:
`docs/performance-evidence/2026-09-26-editor-cleanup-release/diagnostic/`

## Lifecycle and correctness

The native lifetime fixture passed 49 assertions. It created 1,100 counted COM
objects, released all of them on their owning STA, left zero alive, coalesced
requests, rejected cross-thread use and active-update shutdown, recovered from
an injected cleanup failure, and preserved retained editor text and selection.

Framework regression tests cover failure reporting and recovery, one-owner
enforcement, undo, selection, and enabled input-method support. These automated
checks do not replace visible typing, installed-IME composition, or screen-reader
acceptance.

Evidence:
`docs/performance-evidence/2026-09-26-editor-cleanup-release/lifecycle/result.json`

## Release performance

The seven-sample `Test-Performance.ps1 -IncludeLayoutEditors` comparison against
published baseline `78c88fb` passed all 32 budgets and is marked
`releaseEligible: true`. Both editor revisions use the exact candidate policy
source. Editor mount time was +1.89%, update time -2.08%, mount allocation
+0.13%, and update allocation -3.29%. Component work matched. Full-list,
virtualized, and themed full-list baselines remained alongside the editor case.

Evidence:
`docs/performance-evidence/2026-09-26-editor-cleanup-release/release/summary.json`
