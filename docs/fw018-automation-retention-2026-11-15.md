# FW-018 accessibility peer attribution - November 15, 2026

Status: controlled peer reproduction and six alternating soaks complete. Manual focus acceptance remains open.
Framework library code and accepted performance baseline remain unchanged.

## Ownership boundaries and reproduction

A diagnostic sample uses only its own window handle for a native UIA query on an
MTA worker. It then enumerates the WPF peer tree and deliberately holds one managed
peer while disposing the framework host. This is a controlled peer-root experiment;
it does not impersonate the unknown external client from the prior dump.

Three fresh processes reproduce the same retained-object counts. No peer and
query-then-release controls leave zero tracked objects. Holding a row, list or editor
peer retains all 10 realized editors, their 16 KiB payloads and popup objects.
Hosts, components and logical payloads are collected. All native release callbacks
complete and owned native bytes are zero even while editor objects remain reachable.
Releasing the held peer releases every tracked editor. Refreshing only the held
row's child cache releases one editor and leaves nine; refreshing all previously
realized row peers releases all 10 while the selected row peer remains held.

The prior failure dump has an actual ref-counted ElementProxy root through a row
peer, parent list peer, weak-item cache, another row peer and TextBoxAutomationPeer.
The ItemAutomationPeer's weak item reference does not make its cached child peers
weak. The controlled managed root models that downstream graph, not COM client
lifetime or provider disconnection.

## Relationship to earlier growth

Six historical native checkpoints (start/end/quiet from the initial and longer
traced campaigns) have no surviving LifetimeEditor instances, ElementProxy objects
or VirtualRowAutomationPeer instances. LifetimeEditor delegate type-name matches
are not editor instances. The earlier unpaused outliers also passed zero-survivor
checks, but have no exact outlier heap dump. We cannot retroactively assign their
handle/GDI/USER steps to a proxy, or exclude transient automation initialization.

The current reproduction keeps native resources released while retaining managed
editors, and dropping the managed roots does not restore all process handle counts.
UIA initialization, runtime workers and process-wide caches need separate attribution.
Alternating fresh-process memory workloads with and without a one-time, fully released
UIA query address that narrower question. They are not a renderer optimization or
hosted performance release gate.

## Possible remediation boundary

Refreshing cached peers after native row detachment is a candidate mitigation;
the experiment is not a production fix. It must cover all previously realized
row caches, preserve live discovery/realization and avoid constructing peers for
unqueried rows. Screen-reader semantics, reentrancy and large-list costs require
focused correctness and paired performance evidence before changing the renderer.
An external client directly holding an editor peer can intentionally keep its owner
alive; framework cleanup cannot promise collection of externally referenced objects.

Sources: [WPF ItemAutomationPeer](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ItemAutomationPeer.cs)
and [WPF AutomationPeer](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Automation/Peers/AutomationPeer.cs).
These are upstream source references, not proof of the exact installed build; local
.NET 10.0.13 experiments provide behavioral evidence.

## Alternating soak evidence

Six fresh processes run clean/warm, warm/clean, clean/warm order using identical
binaries, 500 rows, four workspace cycles, three warmup plus 180 measured batches.
Warm performs one HWND-scoped native query and verifies all tracked objects released
before entering the ordinary memory workload. No global UIA event subscription or
other application's window is inspected. All 1,098 raw samples match reports;
4,392 workspace cycles pass ownership, bounded-reporting and shutdown checks.

Clean handle endpoints are 453 to 450, 453 to 450 and 453 to 449; GDI stays 20 and
USER stays 17. Warm endpoints are 544 to 555, 545 to 532 and 545 to 542. The two
warm runs without activation retain 20 GDI and decrease USER from 20 to 19.
Warmup includes client/provider/runtime initialization; the higher handle count
is not allocated specifically to the renderer or attributed to one handle type.

Warm repetition 1 gains focus at batch 166 (activation count 0 to 1, active/focused
true). GDI increases from 20 to 26, USER from 18 to 28, handles from the preceding
lower level to 555. USER settles to 27 after focus departs at batch 170. All tracked
editor counts stay zero. This resembles the earlier +6 GDI/+8-9 USER steps, but
establishes correlation, not the originating activation action or native allocation
stack. The first two warm runs decrease collected-heap endpoints; the third warm
and third clean runs have heap steps. No inferred cache size is subtracted.

The reporting summary previously omitted activation/focus when classifying observed
interaction, despite those fields being in raw samples. It now includes them and
reports whether legacy samples have activation evidence. Fixtures reject missing,
nonboolean or decreasing activation fields; old protocols remain readable. The
old summary is preserved separately; warm repetition 1 is no longer labeled quiet.

## Deliberate focus attempt and remaining work

A second sample build adds `--focus-at-batch 60`, restricted to memory mode, with
strict activation and editor-focus assertions. Its matched control completes 183
batches (732 workspace cycles). The focus case stops at batch 60 because Windows
rejects `Window.Activate()`. It is a failed setup, not a successful focus experiment
or a framework lifetime failure. The remaining scheduled focus cases were not run.
The failed report, 60 preceding samples and logs are preserved. A manual title-bar
activation was offered; no manual result is claimed in this evidence set.

Next, deliberately activate the window and verify whether the GDI/USER step recurs
while managed editors still collect. Then investigate the process resources created
by focus/IME/UIA separately from indirect editor retention. Do not label the old
outliers explained, or finalize budgets from these diagnostic campaigns.

## Validation and reproduction

The final sample build repeats seven peer cases in three fresh processes (21 cases),
verifying retained-control sensitivity, single-row/all-row cache contrast, balanced
native release and collection after releasing roots. The deliberate-retention negative
control and both parser fixture suites pass. All 172 framework tests pass. No library
implementation changed and no hosted performance gate or speed improvement is claimed.
The sample's failure diagnostics/checkpoints and explicit interaction workload are
measurement tools, not production cache invalidation.

With the recorded .NET SDK selected, use a new report path for each invocation:

```powershell
dotnet samples/NativeLifetime/bin/Release/net10.0-windows/NativeLifetime.dll --mode automation-retention --report artifacts/new-peer-report.json
dotnet samples/NativeLifetime/bin/Release/net10.0-windows/NativeLifetime.dll --mode memory --report artifacts/new-clean-report.json --rows 500 --batches 180 --cycles 4 --warmups 3
dotnet samples/NativeLifetime/bin/Release/net10.0-windows/NativeLifetime.dll --mode memory --report artifacts/new-warm-report.json --rows 500 --batches 180 --cycles 4 --warmups 3 --automation-warmup
```

[Evidence archive](verification-evidence/2026-11-15-automation-retention/README.md)
keeps the peer/soak/focus-build protocols separate, source and binary manifests,
raw samples, failed setup and historical dump summaries. Historical raw dumps stay
local and the prior proxy-root evidence remains in the screenshot comparison archive.
