# Editor wait isolation — September 25, 2026

Follow-up: [native COM cleanup analysis](editor-com-cleanup-2026-09-25.md) identifies the UI/finalizer cleanup interaction and measures an isolated mitigation. The original investigation below is retained as historical evidence.

## Captured failure

The hosted baseline editor process exceeded 120 seconds on measured sample 6. Dump collection succeeded. At capture, its UI thread was in `TextBox.OnTextPropertyChanged`, through `TextStore.RequestLock`, `TextServicesDisplayAttributePropertyRanges.OnEndEdit`, and `ITfProperty.EnumRanges`. The top managed/native boundary was COM interface conversion followed by a native wait. Process CPU time was only 9.297 seconds over 120.003 seconds elapsed. This identifies the blocked path, not the native lock owner or the cause of the wait.

The [stack](performance-evidence/2026-09-25-editor-wait-isolation/github-baseline-stacks.txt) and [failure metadata](performance-evidence/2026-09-25-editor-wait-isolation/github-failure.json) are preserved without committing the 328 MB dump. WPF's [upstream implementation](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/TextServicesDisplayAttributePropertyRanges.cs) enumerates text-service display-attribute ranges from this callback.

## Native subtraction experiments

`samples/EditorWaitDiagnostics` references WPF only, with no framework dependency. Each completed run retains 1,000 editors, 50 updates and 10,000 actual text replacements, checks final text, and reports UI-thread update allocations. It follows the native reproduction's hidden HwndSource, UniformGrid, ScrollViewer and alternating widths. It is **not** the AdaptiveGrid release workload and does not report framework component work or a mount comparison.

Two fresh processes per mode ran in forward then reverse mode order. Diagnostic variants intentionally remove behavior to isolate causes; they cannot replace the release benchmark or establish a product speedup. There are no discarded warmups in this exploratory campaign. All raw logs, failed attempts, reports and the exact source for each campaign are retained under [the evidence directory](performance-evidence/2026-09-25-editor-wait-isolation/subtractions/Program.cs).

| Native mode | First update total | Second update total | Interpretation |
| --- | ---: | ---: | --- |
| Normal | 50.19 s | 36.19 s | Framework-independent delay reproduced. |
| No caret reads/restoration | 97.70 s | 34.12 s | Caret handling is not necessary for long delays. |
| Fixed read-only/undo settings | Timeout | Timeout | Repeated option toggling is not necessary either. |
| Input-method processing disabled | 34.76 s | 30.52 s | Long final-update pauses remain; not a production fix or proof all TSF activity was disabled. |

Completed runs allocated approximately 819.6–819.9 MB on the UI thread. These values include diagnostic logging. The fixed-options runs did not complete and have no comparable total. Input support remains enabled in production and release benchmarks.

## Pinpointing the time

A phase-instrumented ordinary native run took 39.65 seconds. Its final update spent 20.397 seconds in the setter loop, 0.259 seconds awaiting the dispatcher and 0.221 seconds in explicit layout. A further instrumented run took 37.99 seconds; its final update attributed **22.399 seconds to `TextBox.Text` assignments**, only **1.6 ms to caret reads/restoration**, and **10.6 ms to read-only/undo setters**. Earlier text-replacement steps took about 23–67 ms in total, except step 40, which took 6.441 seconds. Both timing stages have separate logs and source snapshots in `phases` and `setters` beneath the evidence directory.

These measurements locate substantial intermittent latency inside native TextBox text assignment. They do not show that every slow setter waited in exactly the same native frame; the dump supplies a snapshot of one hosted stall, while these timings come from separate local processes.

## Text-length hypothesis

The last update grows the ordinary revision label from 9 to 10. A further two-process-per-mode, reversed-order experiment kept revisions at two digits throughout (`00` through `10`). Normal runs took 24.61 and 17.05 seconds overall, with final steps of 11.37 and 9.24 seconds. Fixed-width runs took **68.26 and 66.99 seconds overall**, although their final steps fell to 0.42 and 0.24 seconds. Changing text length affects where the cost appears, but constant-length text does not remove the delay and made this complete native workload slower. Preserve the original release strings. Exact logs, reports and source are in the `width` evidence subdirectory.

## Reproduction

```powershell
./tools/Test-EditorWaitDiagnostics.ps1 -OutputDirectory artifacts/editor-wait-new-run
./tools/Test-EditorWaitDiagnostics.ps1 -Modes normal,fixed-width -OutputDirectory artifacts/editor-width-new-run
```

Run from PowerShell. The runner builds once, launches modes sequentially, reverses their order on the second repetition, enforces a 120-second diagnostic process limit and retains each outcome. Its successful exit means the experiment finished, not that all processes passed; inspect `*-process.json` for timeouts. `fixed-width` preserves text replacement count but changes label formatting to test the revision 9-to-10 length increase. Neither this formatting change nor any subtraction is applied to the release harness.

## Decision

No runtime optimization, input-support change, budget increase or baseline advancement is justified by this investigation. NuGet remains blocked by the incomplete release comparison. The next useful evidence is a native stack/wait-owner analysis of the captured COM wait (for example in WinDbg), rather than another speculative change to renderer caret handling or layout. Managed dump analysis alone does not identify the native wait owner.
