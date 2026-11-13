# FW-010 real WebView2 initialization and popup races — October 29, 2026

The first real-browser acceptance slice passes **15/15 cases**: three fresh-process
repetitions of five cases, each with its own profile. This adds a sample and test
driver; framework runtime/APIs and GuidedGrade implementation are unchanged.
FW-010 remains in progress for the broader browser acceptance matrix.

## Environment and ownership

- .NET SDK 10.0.402 / runtime 10.0.13; WebView2 SDK 1.0.4314.35;
  installed browser runtime 156.0.4314.50.
- Source base: framework `64bd242`, with uncommitted acceptance sample/tooling.
  Exact source hashes and a frozen source archive accompany the final run.
- Real WPF windows, with an owned child window, use the existing `WpfUI.Native`.
  The application owner drains initialization and popup tasks, then disposes the
  host. Its release callback asserts native detachment and disposes WebView2 once.
- Every process gets a new profile below the evidence output directory; the
  provider's actual UserDataFolder is checked. No GuidedGrade profile is reused.
  Local parent/child pages are checked by URL and document title. Navigation outside
  the local pages folder is rejected; this is not a browser network sandbox.
- Parent and child explicitly share one CoreWebView2Environment. Successful popup
  assignment and child page load verify this setup rather than inferring it from
  a shared directory name.

## Cases and observed outcomes

| Case | Ordering verified | Repetitions |
| --- | --- | ---: |
| Environment-to-control boundary close | Hold application continuation after requesting the real environment; request close; resume; no control initialization starts | 3/3 |
| Control initialization close | Actual EnsureCoreWebView2Async is pending when close is requested; drain, detach, release exactly once | 3/3 |
| Popup success | Real NewWindowRequested, deferral, shared environment, one assignment and successful local child page load | 3/3 |
| Parent close during popup initialization | Child initialization is actually pending; parent requests close; drain child and popup; zero assignments after close, one deferral completion | 3/3 |
| Child close during popup initialization | Child initialization is actually pending; child requests close; popup continuation does not assign disposed/closing child; parent subsequently closes | 3/3 |

Every final process reports completed dispatcher shutdown and closed framework COM
cleanup policy. All created browser hosts release once; no unexpected initialization,
popup or process-failure diagnostics were recorded. Script completion is explicitly
observed even when parent close interrupts it. A held pre-assignment continuation
makes the application close ordering deterministic; this is not random stress.

The environment-boundary case is deliberately **not** a pending environment-provider
race: CreateAsync completed synchronously on this machine. The original probe
rejected that result in two pilot runs, and those failures are retained. The revised
test names and reports distinguish the controlled application boundary from actual
pending control initialization. A first five-case pilot and intermediate repeated
campaigns are retained alongside the final owned-window campaign.

## Validation and reproduction

```powershell
pwsh -NoProfile -File tools/Test-WebViewAcceptance.ps1 -OutputDirectory artifacts/webview-new-run
```

Use a fresh output directory, PowerShell 7, .NET 10 and an installed WebView2 Runtime.
The [sample guide](../samples/WebViewAcceptance/README.md) explains the cases and
limits. Each subprocess has a 120-second driver limit; timeout fails the test and
retains evidence. This bound is not a production provider cancellation policy.

Final sample build passes with zero warnings/errors. The full release regression
run during this change passes 172 tests, 15 visual checks, 21 stress assertions,
startup/editing/disposal and controlled WPF native-window shutdown. After that
build, explicit owned-window linkage was added to the browser sample and all 15
browser cases were rebuilt and rerun successfully. Core/WPF source is unchanged.

No new Test-Performance comparison was run: these changes add acceptance tooling,
local HTML and documentation, not framework runtime behavior. Existing hosted
32/32 evidence for `64bd242` is preserved; it is not claimed as a WebView2 memory or
latency benchmark. Browser execution remains an explicit command, not a newly
enabled CI release gate. The solution build includes the sample.

[Preserved reports, source and hashes](verification-evidence/2026-10-29-webview2/README.md).
Profiles remain locally under `artifacts/webview-acceptance-2026-10-29`; archives
exclude browser profile/cache contents and compiled binaries.

## What this establishes and what remains

The existing framework host supports this tested, application-owned drain-before-
release policy with real WebView2. It does not require a new framework browser API.
GuidedGrade's current synchronous Closed cleanup and popup completion logic are
not replaced or validated by this separate sample; see the [source review](webview2-review-2026-10-29.md).

Remaining FW-010 evidence: real focus/keyboard and native-airspace behavior, theme
changes, initialization/navigation/process failure and recovery, script-requested
close, and browser resource/process retention. Uncooperative provider timeout
behavior and error-resilient production cleanup also remain outside this successful-
provider slice. Do not claim arbitrary forced disposal during initialization, SSO,
live grading transfers, leak freedom, or browser-process exit from these results.
