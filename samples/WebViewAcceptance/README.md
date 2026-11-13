# Real WebView2 acceptance

This is a framework verification sample, not a production browser wrapper. It uses
the existing `WpfUI.Native` contract. WebView2 is a sample-only package dependency;
the core and WPF libraries do not depend on it.

Run from the repository root with PowerShell 7, .NET 10 and an installed WebView2
Runtime. Use a new output directory every time:

```powershell
pwsh -NoProfile -File tools/Test-WebViewAcceptance.ps1 -OutputDirectory artifacts/webview-new-run
```

Each case runs in a new process with a fresh profile below its output folder.
Parent and child HTML are local files. The browser rejects navigation outside that
pages folder; the test does not use GuidedGrade accounts, cookies or grading pages.
WebView2 runtime background activity is not controlled by this navigation policy.
Profiles and failed runs are retained for diagnosis, not deleted or reused.

The five default cases are environment-to-control boundary close, close while actual
control initialization is pending, successful popup, parent close during child
initialization, and child close during its initialization. A held application
continuation makes pre-assignment ordering deterministic. The two popup races
also require real initialization to be pending when close is requested. The
environment boundary uses an explicit gate; it does not claim asynchronous
environment creation when the provider completes synchronously.

The owner defers window close, observes initialization and popup tasks, completes
popup deferrals and detaches/releases the browser exactly once before dispatcher
shutdown. Parent and child explicitly share the same environment. This is a
drain-before-release policy, not a claim that provider initialization is cancellable.
The test driver bounds each process to 120 seconds; a timeout is a failure, not
successful cleanup or a production shutdown policy.

The Release solution build compiles this sample. Browser execution remains an
explicit acceptance command because it requires an installed runtime. It is not
silently added to the standard release/performance gate. Later work still needs
repeated focus/keyboard and native-airspace acceptance, failure/recovery cases, resource and
browser-process retention measurements, and integration of the policy into the app.

## Optional foreground interaction

`focus-keyboard` and `airspace` are explicitly interactive: they activate a test
window, send real keystrokes to the local page, and save cropped screen captures.
Do not run them on someone's active desktop without their agreement. The normal
command above excludes them. To opt in from PowerShell 7 when ready:

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes focus-keyboard,airspace -IncludeInteractive -OutputDirectory artifacts/webview-interactive-new-run
```

The runner rejects these modes without `-IncludeInteractive`; direct executable
use requires `--interactive`. Input requires an x64 process, foreground ownership
and no held physical modifiers. The window waits up to 60 seconds for activation;
losing foreground focus fails the test. Native browser coverage of WPF sibling
overlays is an expected limitation, checked against a hidden-browser control and
a separate popup HWND. This is not a composition-control or arbitrary-overlay test.

## Explicit failure injection and recovery

These modes are not part of the default command. They deliberately fail only test
resources. Run from PowerShell 7 with a fresh output folder:

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes initialization-recovery,runtime-missing-recovery,navigation-recovery,renderer-recovery,browser-recovery -OutputDirectory artifacts/webview-recovery-new-run
```

`initialization-recovery` uses a new file occupying the test profile path and
requires the application preflight to reject it before the provider is called.
An earlier unguarded run timed out, so this is not claimed as provider-failure
coverage. `runtime-missing-recovery` supplies a nonexistent runtime folder and
requires the real provider's WebView2RuntimeNotFoundException. Both release the
failed owner and create a fresh owner with a valid profile. Navigation targets a nonexistent local file,
then loads the valid page using the same controller. Renderer/browser cases
terminate exactly one process returned by the test environment's GetProcessInfos,
after checking its isolated profile, process kind, executable name and, for the
browser, its controller PID. The process handle is retained; no name-wide or tree
termination is used. No running personal browser is selected.

Renderer failure must produce RenderProcessExited and recover on the same controller.
Browser failure must produce BrowserProcessExited, wait for the environment's exit
notification, release the failed owner, and recreate a working browser with the same
isolated profile. Every case requires actual local-page script execution after
recovery and exactly-once host release. Expected failures remain in reports/logs.
The sample caches its subscribed core object because the control's CoreWebView2
getter throws after browser-process failure; cleanup attempts remaining actions and
surfaces accumulated failures. This is explicit application recovery policy, not
automatic framework recovery or proof that arbitrary site state survives a crash.


## Bounded waits and script-requested close

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes stalled-success-close,stalled-fault-close,script-child-close,script-parent-close -OutputDirectory artifacts/webview-shutdown-new-run
```

`StartWithin` bounds the initialization caller's wait. On timeout it marks the
owner closing and starts draining the original operation. `CloseWithin` bounds
waiting for that drain; a timeout does not dispose the pending controller or
permit dispatcher shutdown. Repeated close requests return the same completion
task. The application must observe `CloseCompletion`, including late faults, and
keep the dispatcher alive until it settles. `Timeouts` exposes the two diagnostics.
A fresh owner is required for retry; starting an existing or closing owner fails.
A permanently stuck provider still needs an explicit process-level application
policy; this sample does not forcibly shut down an in-process provider.

Stall tests inject an incomplete environment task and use a 100 ms test deadline.
They verify no early release, no late controller attachment, late fault propagation,
exactly-once release and fresh-owner retry. They do not simulate a blocked UI thread
or prove that an actually hung provider can recover. The success continuation uses
a real environment after the controlled deadline; the fault is synthetic.

Real local parent/child pages call `window.close()`. `WindowCloseRequested` uses the
same yielded close/drain path as the window close button. Closing a child keeps its
parent usable; closing a parent drains and releases its owned child. Event handlers
are removed using the cached core reference before host disposal. These cases need
no foreground keyboard input and remain explicit modes rather than default cases.


## Browser resource retention

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes retention-fresh,retention-reused -OutputDirectory artifacts/webview-retention-new-run
```

Each mode creates and normally closes 44 local parent/popup pairs (four warmup,
40 measured), plus one deliberately retained pair used to validate the collection
probe. The default three repetitions run each mode in a fresh process. Fresh mode
uses a new isolated profile per cycle; reused mode reopens one profile after its
previous browser process exits. Neither mode activates windows for keyboard input.

Before close, capture only processes reported by the exact isolated environment
and retain their OS handles. After close, require the matching BrowserProcessExited,
normal exit of every captured process and an empty environment process list. No
process is killed to satisfy the retention checks. Dispose all diagnostic handles.
Track weak references to each owner, window, host, WebView2 and core, plus the shared
environment. After a fixed three dispatcher/finalizer/GC passes, all must be dead.
The negative control must retain its owner while rooted, then collect after release.

Each cycle saves process memory while live, UI-thread allocations and elapsed time,
then natural and collected host heap/private bytes/working set/handles/GDI/USER
counts. These observations are not performance comparisons or memory release gates.
Profile caches persist intentionally and are excluded from the evidence archive.
Per-cycle JSON is written before survivor assertions so a failure retains evidence.


## Final interactive acceptance and support boundary

```powershell
./tools/Test-WebViewAcceptance.ps1 -IncludeInteractive -Modes focus-keyboard,airspace,pointer-bounds -OutputDirectory artifacts/webview-interactive-new-run
```

The pointer case waits for three real actions in the specifically titled test window:
click **Before browser**, click the local page's **Activate**, then scroll the blue
page down. Each step has a 60-second limit within the driver's unchanged 120-second
case limit. It checks trusted DOM pointer input/focus, scroll offset, and blue inside
versus white at the left margin outside the allocated browser bounds. It does not
claim arbitrary WPF clipping support. Keyboard acceptance applies three themes and
requires unchanged core identity, DOM focus, text and selection each time.

See the [v1 permanent-stall and interaction contract](../../docs/native-provider-stall-policy.md).
A provider that never settles leaves close pending; a hard shutdown deadline needs
application-owned process isolation. Forced in-process disposal is not supported.
