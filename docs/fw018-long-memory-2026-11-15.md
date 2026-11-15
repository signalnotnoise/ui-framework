# FW-018: handle identity and longer memory runs — November 15, 2026

Status: investigation in progress; no memory budgets or release baseline changed.
Framework libraries are unchanged. This continuation extends acceptance tooling
and keeps normal runs separate from paused/traced diagnostics.

## Handle identity

The two persistent browser file handles in the successful identity capture refer
to the installed .NET 10.0.13 desktop assemblies `System.Private.Windows.Core.dll`
and `System.Windows.Primitives.dll`. Both were opened by the thread identified in
the dump as `.NET Tiered Compilation Worker`, and both remain at the quiet
checkpoint. File type and final-path queries succeed. This identifies those
handles in this capture; it is not a retrospective identity proof for every older
handle-count increase.

Native event/I/O-completion creation records identify .NET worker/finalizer
threads. Counts include turnover: a new handle value is not necessarily net
growth. In the longer diagnostic, total handles are 477 → 474 → 476. Some ALPC and
timer handles disappear while events/completion ports appear. The non-free heap
decreases from 2,106,022 to 2,077,670 and then 2,076,570 bytes. These paused captures
are attribution evidence, not unperturbed budget measurements.

The x64 NT history collector uses a bounded 131,072-entry ring on its explicitly
selected test process. Known file/event controls verify handle values, process
IDs, open/close operations and disk-file identity. However, Windows returns empty
creation stacks even for the controls. The first control that required stacks
failed; the tool now reports metadata validation and stack availability separately,
with an explicit warning. Exact native allocator call stacks remain unavailable
in this session. No privileges, registry settings or security settings were changed.

One broad file-query pilot timed out. The blocking call was not captured, so it
is not presented as a diagnosed framework fault. Its artifacts remain preserved.
The corrected probe restricts live inspection to added file handles, never queries
pipe state/data, and resolves paths only for disk handles. A transient Mojo-thread
file was already closed between dump and query (error 6); that race is recorded
as unavailable identity, not a persistent leak or a successful query.

The diagnostic collector also records window classes belonging to the test
process. In the longer native capture, the six enumerated window identities remain
the same (five WPF wrappers and one IME window), while USER counts vary 18 → 19 →
20. Window enumeration does not cover every USER object or message-only window;
it cannot explain all of that difference. No input, activation or keyboard focus
was observed in that diagnostic run. Dump/trace perturbation remains a limitation.

## Longer native checks and reported interaction

The first campaigns use three fresh processes at each of 500 and 5,000 rows,
with three warmup plus 180 measured batches and four workspace cycles per batch.
All six runs pass original collectability, native-buffer, visited-state/removal,
reporting-root and shutdown checks. The 5,000-row checkpoint retains the intended
5,000 logical states with 10–11 native editors, then releases all tracked state
after removing keys and disposing the host.

The 500-row runs have collected-heap endpoint changes of −5,344 / +6,616 / −5,384
bytes and handle changes of −3 / +26 / −3. The second run has a late GUI-resource
step (+6 GDI and +8 USER objects). This result is preserved, not retried away.
The 5,000-row changes are −10,448 / −6,304 / +52,640 heap bytes and −6 / −4 / −7
handles. The prior type-cast cache finding is a possible explanation for a step,
but has not been proven for that particular 52,640-byte change.

The user reported hovering over a test window and seeing slower scrolling. The
exact run/batch was not established. Routed input and repaint work can overlap the
scripted workload; this observation is not proof of the resource-step cause.

A bounded passive `InputObservation` now records counts/booleans for its native
test window: pointer entry/movement, button/key events and activation/focus state.
It never moves the pointer, suppresses input or records key values/text. It is a
WPF-window observer, not a global input log or a native-browser-HWND monitor.
The analyzer validates the required input evidence and distinguishes observed
interaction from ownership success. Missing input evidence never means untouched.

Three more 500-row/180-batch runs pass ownership. Run 1 records 19 routed key-down
events, no pointer entry, +73,256 heap bytes, +23 handles, +6 GDI and +9 USER objects.
Its GUI step starts before the key events; causation remains unresolved. Runs 2
and 3 record no routed input, have heap changes of −4,432 / −4,856 bytes and handle
changes of −6 / −8, with no GDI/USER growth. Activation/focus fields were added
after that campaign and are present in the longer diagnostic; exact source
snapshots preserve that distinction. These repeats are not an overhead comparison
with the earlier protocol, and no timing improvement is claimed.

## Longer browser campaign

All six cases pass: three fresh-profile and three reused-profile processes,
each with 132 cycles (four warmup and 128 measured). This covers 792 cycles,
plus six deliberate-retention controls. Raw sample files match reconstructed
reports; tracked controls are collected and isolated browser processes exit.
The explicit 900-second deadline applies only to this longer workload.
Defaults and performance-gate budgets are unchanged.

Fresh-profile heap endpoint changes are +94,416 / +56,952 / +95,480 bytes;
handle changes are +24 / +35 / +8. Reused-profile heap changes are
+40,208 / +91,376 / +40,240 bytes; handles +1 / +5 / +2. GDI counts are flat;
USER changes range +1 to +5. These whole-process increases are not all attributed
by the separate DLL-handle capture, and do not establish an approved plateau.
Native browser input was not monitored.

## Verification and preserved evidence

The combined analysis validates 15 runs and 2,442 spooled samples against reports,
including 6,588 native workspace cycles and the 792 browser cycles. Framework
source hashes match across these campaigns. Full Release validation passes
172 tests, 15 visual checks and 21 stress assertions, together with its reporting,
short native lifetime and shutdown checks. Native-memory and dump-parser fixtures
pass. This is local correctness/diagnostic evidence, not a hosted performance gate.

The [evidence archive](verification-evidence/2026-11-15-long-memory/README.md)
preserves reports, raw samples, source snapshots and failed-pilot logs. Full dumps
and browser profiles remain local; dump hashes identify the retained captures.

## Completion requirements

FW-018 stays open until representative budgets and useful contextual failure
diagnostics have supporting acceptance evidence. These observations do not approve
a whole-process browser ceiling or prove the absence of every OS/provider leak.
Do not subtract inferred cache sizes, discard the interactive/outlier runs, or
describe lower component counts as a latency improvement. Controlled focus/IME/
hover acceptance is still needed to separate initialization from continuing growth.
The existing renderer preserves original/aggregate failures but still lacks the
owning component/key context identified in the earlier FW-018 review.

## Reproduction

In PowerShell 7 with the recorded .NET SDK/runtime selected, use new output folders:

```powershell
./tools/Test-NativeLifetime.ps1 -Batches 180 -Rows 500 -Repetitions 3 -CaseTimeoutSeconds 900 -OutputDirectory ./artifacts/native-180-new
./tools/Test-NativeLifetime.ps1 -Batches 180 -Rows 5000 -Repetitions 3 -CaseTimeoutSeconds 900 -OutputDirectory ./artifacts/native-5000-new
./tools/Test-WebViewAcceptance.ps1 -Modes @('retention-fresh','retention-reused') -RetentionCycles 132 -Repetitions 3 -CaseTimeoutSeconds 900 -OutputDirectory ./artifacts/browser-132-new
./tools/Test-MemoryAttribution.ps1 -Mode native -NativeBatches 180 -TraceHandles -OutputDirectory ./artifacts/native-traced-new
python tools/Summarize-MemoryAttribution.py ./artifacts/native-traced-new --handle-records --output ./artifacts/native-traced-new/attribution.json
```

The trace command requires the local dotnet-dump tool (or `-DumpToolPath`). Raw
dumps and browser profiles stay ignored/local. Sources, reports and summaries are
kept separately from memory dumps. See the [backlog](backlog.md) and
[preceding attribution](fw018-heap-handle-attribution-2026-11-13.md).

Structure/API references: [NT process handle tracing definitions](https://github.com/winsiderss/phnt/blob/master/ntpsapi.h),
[Microsoft minidump definitions](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/minidumpapiset.h),
[final file-path queries](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew),
and [WinDbg handle tracing](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/-htrace).
