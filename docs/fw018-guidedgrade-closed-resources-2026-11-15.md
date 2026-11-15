# FW-018 native resources with GuidedGrade closed — November 15, 2026

Four accepted diagnostic campaigns completed; two excluded attempts are preserved. FW-018 remains open: three aggregate GDI
objects still lack individual identities. No framework library, release baseline
or memory budget changed.

## Conditions and evidence

GuidedGrade/Lab Feedback process-name checks found no matching app at launch,
every capture and exit. The effect therefore does not require GuidedGrade;
its impact on earlier timings and other background applications is not measured.

Manual focus, untouched control and matching dedicated-STA-thread focused/untouched runs each use
500 rows, four workspace cycles, three warmup and 600 measured batches. All
2,412 raw samples match reports: 9,648 workspace cycles pass tracked ownership,
bounded-reporting and shutdown checks. Both manual runs record one activation,
no client-area mouse-down and no key-down. Both accepted controls record no interaction.
The user confirmed both title-bar clicks.

All 172 framework tests pass, along with native-memory/dump-parser fixtures and
the GDI ABI controls. No hosted performance gate was run for this diagnostic work.

Snapshots and full dumps pause these runs. They are attribution experiments,
not timing comparisons or unperturbed memory budgets. Source snapshots distinguish
the builds; the UI-thread variant intentionally changes process topology.

## Identified resources

Ordinary focus raises GDI from 20 to 26 and USER from 18 to 27. Shared-table
entries increase from 14 to 15: one new font. New windows include two
`CiceroUIWndFrame` windows and `MSCTFComposition` owned by `msctfui.dll`,
`MSCTFIME UI` without a resolved module, and `SystemUserAdapterWindowClass`
owned by `CoreMessaging.dll`. These identities connect part of the step to
Windows text-input infrastructure, not retained framework editors.

After dispatcher shutdown the new font and text-input UI windows are gone.
CoreMessaging and `OleMainThreadWndClass` from `combase.dll` remain: 13 GDI /
8 USER. The untouched reference has 10 GDI / 4 USER and only the COM window.
Both enumerate ten GDI objects of the same types: four regions, two DCs, two
fonts, one bitmap and one brush. Matching types are not matching identities
across processes; handle values can be reused.

The dedicated STA run also reproduces 20 to 26 GDI. Dispatcher shutdown leaves
13 GDI / 10 USER and three windows (CoreMessaging and two COM windows).
After its UI thread exits while the process remains alive, CoreMessaging and
that thread's COM window disappear. One COM window remains on the other thread,
with 13 GDI / 6 USER. The CoreMessaging window has a thread-lifetime explanation;
the aggregate excess remains at UI-thread exit; individual identities of those
three objects are not tracked.

The fresh same-binary dedicated-thread control (`thread-control-untouched`) passes
all 603 raw samples / 2,412 workspace cycles with no activation, pointer or keyboard
input and GuidedGrade absent at launch, every capture and exit. It ends at 10 GDI /
6 USER after dispatcher shutdown, then 10 GDI / 4 USER after thread exit. Kernel
handles fall from 482 at baseline to 410 after thread exit. Both thread-exit
inventories enumerate ten GDI objects. Thus the extra STA-thread arrangement does
not explain the focused run's aggregate +3 GDI / +2 USER; it still does not establish
a framework leak or identify the unenumerated objects.

Two attempts are excluded: `thread-control` detected GuidedGrade only at its final
exit audit; `thread-control-clean` recorded one activation and pointer movement and
was stopped at the user's request. Their evidence is retained outside accepted
totals. No workload or acceptance assertion was relaxed for the successful rerun.

Kernel-handle totals include runtime/background activity. The ordinary focused
run has 475 at baseline, 504 at activation and 427 after shutdown; the control
has 477 at baseline, 466 at end and 407 after shutdown. Type summaries preserve
events, completion packets, files, ports, threads and unknown descriptors.
Count differences alone establish neither allocation stacks nor ownership.

## Inventory limits

The collector reads the selected process's native-x64 PEB shared GDI table using
the PHNT layout. It guards bitness/PID truncation and read sizes, never follows
kernel pointers and never changes target handles. Known owned brush, pen and
memory-DC controls validate exact handles/types and cleanup. This undocumented
ABI is tied to the recorded Windows build, not a portable framework API.

Coverage is partial: baseline GDI 20 exposes 14 entries, focused 26 exposes 15,
and post-shutdown 13 exposes 10. Five of six newly counted GDI objects and the
three post-shutdown extras lack individual identities. Window enumeration includes
hidden/thread and message-only windows, but not every USER object kind. Module
names match GWLP_HINSTANCE against loaded modules, not creation stacks. Counts,
table reads, window enumeration and dumps are not atomic.

The post-shutdown checkpoint keeps the STA thread alive. The thread-exit variant
joins a dedicated STA thread before capturing again. Normal sample runs do not
enable these pauses. No production cleanup or COM policy was changed.

## Remaining work and reproduction

Identify the three GDI objects outside the shared table with a native allocation
trace that has verified coverage. Keep this separate from production automation
peer-cache remediation: tracked editors collect throughout these experiments.
Representative budgets and contextual error diagnostics also remain open.

Build the sample and collector with the recorded SDK, then use new output paths:

```powershell
pwsh -NoProfile -File tools/Test-FocusResources.ps1 -OutputDirectory artifacts/new-focus-trace
pwsh -NoProfile -File tools/Test-FocusResources.ps1 -Control -OutputDirectory artifacts/new-focus-control
pwsh -NoProfile -File tools/Test-FocusResources.ps1 -ThreadExit -OutputDirectory artifacts/new-thread-trace
pwsh -NoProfile -File tools/Test-FocusResources.ps1 -ThreadExit -Control -OutputDirectory artifacts/new-thread-control
```

Manual runs require one title-bar activation after the start capture. No desktop
UIA client is used. Dumps stay local; the [evidence archive](verification-evidence/2026-11-15-guidedgrade-closed/README.md)
contains hashes, metadata, reports and raw samples.

ABI references: [PHNT GDI entries](https://github.com/winsiderss/phnt/blob/master/ntgdi.h)
and [PEB definition](https://github.com/winsiderss/phnt/blob/master/ntpebteb.h).
