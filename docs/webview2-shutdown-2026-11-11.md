# FW-010 bounded waits and script-close — November 11, 2026

The focused campaign passes **42/42 cases**: three fresh-process repetitions of
four new shutdown cases, five prior lifetime/popup cases and five prior recovery
cases. SDK 1.0.4314.35, installed WebView2 runtime 157.0.4341.42, .NET 10.0.13.
The four new cases account for 12 passes. All reports require dispatcher shutdown
and cleanup-policy completion. These are sample-owned policies, not a framework
API or GuidedGrade change. FW-010 remains in progress.

## Implemented policy

- `BrowserSession.StartWithin` bounds the caller wait and initiates the existing
  close/drain path on deadline. It retains the original initialization task.
- `CloseWithin` reports a timeout while retaining the same close task and native
  owner. Repeated close calls are idempotent, including after the window closed.
  The caller must observe `CloseCompletion` and keep the dispatcher running until
  the provider settles. Diagnostics are retained in `Timeouts`; faults propagate.
- An existing/closing owner rejects another Start. Recovery creates a fresh owner.
- `WindowCloseRequested` enters the same close path. Cleanup yields out of the
  native event, drains owned children/deferrals, removes the subscribed event via
  its cached core reference, detaches and releases once.

## Acceptance evidence

`stalled-success-close` and `stalled-fault-close` inject an incomplete environment
operation. Both initialization and close waits expire at a 100 ms test deadline.
The tests assert no release while pending, a visible retained owner, one stable
close task, two diagnostics, and rejection of same-owner restart. They then supply
a real environment or an exact synthetic IOException. Completion must never start
a controller on the closing owner; cleanup must release once, propagate the exact
late fault when present, and allow a fresh owner to initialize and close.

These controlled tasks test application deadline and ownership behavior, not an
actual hung provider or blocked dispatcher. Permanently stalled in-process work
still requires an application process-isolation/termination decision; timeout does
not cancel WebView2. No test kills a provider to make these deadline checks pass.

`script-child-close` and `script-parent-close` use real local parent/child pages
and `window.close()`. Each requires exactly one native close event. Child close
leaves the parent visible and able to execute a title query. Parent close drains
and releases its child. Both require one popup assignment/deferral completion and
one release per owner. No foreground activation, key injection or screenshot was
needed for this campaign. The prior interactive pilot was not rerun.

## Validation and preserved evidence

- Pilot: first build rejected an intentionally unawaited assertion call under
  warnings-as-errors (CS4014). Explicit task discard corrected it; the original
  build log is retained. The next pilot passed 4/4 cases.
- Repeated campaign: 42/42, no test retries or budget relaxation.
- Full framework Release regression passed: 172 tests, visual checks, 21 stress
  assertions, startup/editing/disposal and native real-window lifetime checks.
- Source manifests and raw JSON/logs are retained with an immutable source snapshot
  in [verification evidence](verification-evidence/2026-11-11-webview2-shutdown/README.md).
  Browser profiles, caches and crash dumps are excluded from the archive.
- Core and WPF runtime sources are unchanged. No performance comparison was run;
  this sample/test-only change makes no latency/allocation or release-gate claim.

Run in PowerShell 7 with the repository SDK on PATH and a fresh output directory:

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes stalled-success-close,stalled-fault-close,script-child-close,script-parent-close -OutputDirectory artifacts/webview-shutdown-new-run
```

Remaining FW-010 acceptance: repeated interactive/pointer/clipping coverage,
browser process/resource retention, and an explicit supported scope for a provider
that never completes. GuidedGrade adoption is separate application work.
