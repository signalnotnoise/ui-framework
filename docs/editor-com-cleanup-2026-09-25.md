# Editor stalls: COM cleanup and dispatcher suppression

## Native dump findings

The captured hosted baseline dump now has matching native symbols, resolved using Microsoft's portable `Microsoft.Debugging.Platform.DbgEng` and `Microsoft.Debugging.Platform.SymSrv` packages, both version `20260319.1511.0`. The small [debugger host source](performance-evidence/2026-09-25-editor-com-cleanup/DumpStacks.cpp) is retained; binaries and downloaded symbols remain local artifacts. It accepts a dump path, symbol path and debugger command string. Analysis used `~0s; kv; ~3s; kv`, followed by runtime structure inspection. No live application was attached or modified.

The [symbolized stacks](performance-evidence/2026-09-25-editor-com-cleanup/native-symbolized.txt) and [runtime context fields](performance-evidence/2026-09-25-editor-com-cleanup/cleanup-context.txt) connect the waits:

- UI thread `0x1fc0` is creating a COM runtime-callable wrapper after WPF's text-services range enumeration. It enters `RCWCleanupList::CleanupWrappersInCurrentCtxThread`, then `Thread::JoinEx` and `DispatcherSynchronizationContext.Wait`.
- Finalizer thread `0x22bc` is in `RCWCleanupList::CleanupAllWrappers` / `ReleaseRCWListInCorrectCtx`, entering a COM context and waiting in `combase!MTAThreadWaitForCall`.
- The runtime cleanup list's `m_pCurCleanupThread` is `0x0000022e04d7eb80`, whose OS thread ID is `0x1fc0`. The finalizer's `CtxEntry.m_pSTAThread` points to that same UI thread. Its COM context is `0x0000022e04cf5040`.
- The [captured dispatcher](performance-evidence/2026-09-25-editor-com-cleanup/dispatcher-fields.txt) has `_disableProcessingCount = 1`. WPF's synchronization-context implementation uses a native non-pumping wait in this state.

This identifies a COM-cleanup/message-pumping interaction: the finalizer needs the UI apartment while the UI thread is inside a WPF region suppressing processing. The runtime's cleanup path attempts a short cooperative wait when the finalizer targets that STA. Repeated blocked cooperation can accumulate substantial latency. The dump is not proof of a permanent deadlock: the [runtime implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/vm/runtimecallablewrapper.cpp) uses a short timed join, and completed runs show eventual progress. The cited .NET 10 source explains the mechanism; the dump itself is runtime 10.0.12 and native symbols were matched to that binary. See also [WPF's synchronization-context implementation](https://github.com/dotnet/wpf/blob/v10.0.0/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/DispatcherSynchronizationContext.cs).

This is more specific than the earlier observation of a text-services wait. It does not establish that every timing outlier in every scenario has this cause.

## Controlled experiment

The native-only reproduction now has two additional diagnostic modes. Both keep input methods enabled, all 1,000 editors, 50 updates, 10,000 text replacements and the original variable-length labels:

- `cleanup-before`: call `Marshal.CleanupUnusedObjectsInCurrentContext` before each update and after the last update, leaving automatic cleanup enabled.
- `controlled-cleanup`: additionally call `Thread.CurrentThread.DisableComObjectEagerCleanup` before constructing the test Application. Cleanup occurs at the explicit boundaries above, outside WPF property updates. The entire loop and those cleanup calls are timed, including the last cleanup.

Two fresh processes per mode ran sequentially, with reversed order on repetition two. There were no discarded warmups. These are diagnostic measurements, not framework baseline comparisons. The workload is a native UniformGrid reproduction, not the release AdaptiveGrid benchmark. No component work or mount comparison is claimed.

| Mode | Update time, runs 1 / 2 | Final-step time, runs 1 / 2 | UI-thread allocation, runs 1 / 2 |
| --- | ---: | ---: | ---: |
| Normal | 27.55 / 26.15 s | 12.35 / 11.75 s | 819,464,312 / 819,502,824 B |
| Cleanup before updates | 24.76 / 22.57 s | 10.01 / 9.13 s | 819,506,608 / 819,423,048 B |
| Controlled cleanup | 14.59 / 13.64 s | 0.45 / 0.47 s | 819,445,408 / 819,422,104 B |

The controlled policy was approximately 47–48% faster than its corresponding normal run, with nearly unchanged allocations. It substantially reduced the final-step pause in both repetitions. Merely requesting cleanup before updates retained a large pause. Every process completed and verified all text replacements and final strings. [Summary, raw logs, reports and exact source](performance-evidence/2026-09-25-editor-com-cleanup/summary.json) are preserved. Diagnostic instrumentation adds overhead and two repetitions do not establish a production performance guarantee.

To repeat:

```powershell
./tools/Test-EditorWaitDiagnostics.ps1 -Modes normal,cleanup-before,controlled-cleanup -Repetitions 2 -OutputDirectory artifacts/editor-com-new-run
```

## Integration boundary and remaining checks

No framework renderer, consumer app, release harness, baseline, budget, input support or package version was changed. The experiment does not clear the release gate.

Microsoft documents that [disabling eager cleanup](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.disablecomobjecteagercleanup?view=net-10.0) is thread-wide and cannot be re-enabled on that thread. [Explicit cleanup](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.cleanupunusedobjectsincurrentcontext?view=net-10.0) pumps messages. Therefore it must not be silently enabled by a reusable ViewHost or individual TextEditor.

The next implementation decision is an explicitly application-owned UI-thread policy, with cleanup at safe idle/shutdown boundaries. Before adopting it, verify callback reentrancy, exceptions and shutdown, retained native islands, COM lifetime/memory under sustained use, and real editor/IME behavior. Then measure the actual framework and consumer under an identical declared policy with full-list and themed comparisons. Preserve the original failed runs; do not silently use this diagnostic policy to waive the existing gate or claim a package speedup.
