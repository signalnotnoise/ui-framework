# GuidedGrade WebView2 review — October 29, 2026

FW-010 remains in progress. GuidedGrade already integrates WebView2; a new adapter
is not inherently missing, and this review has not reproduced a framework defect.
The remaining framework deliverable is browser-specific acceptance of the existing
native-host contract, with application ownership kept explicit.

## Reviewed source and limits

Reviewed `C:/Users/iotero/source/repos/GuidedGrade`, base commit
`b7c68a1789b01353f1efa89f1dc1efeb44cc8e71`, including its substantial uncommitted
working changes. This is not a review of the obsolete Lab-Feedback-WPF checkout.
The source fingerprint manifest is [webview2-review-source.json](webview2-review-source.json).
Application files were read only. Source review is not a live browser test, a
compilation result, or proof of login/popup compatibility.

The current project references `Microsoft.Web.WebView2` 1.0.4314.35 and
`SignalNotNoise.UI.Wpf` 0.1.0-alpha.7-local.1. The application's older graph section
still names WebView2 1.0.4258.31 and says no grading integration; current code includes
field mapping and comment/grade transfer. Treat those earlier descriptions as stale.

## Already implemented

- `GuidedGrade/Views/FullSailBrowserView.cs`: a keyed native DockPanel contains the
  WebView2 and a separately owned declarative toolbar/status host. The browser and
  panel are retained by the view/model; no release callback is supplied to WpfUI.Native.
- `ViewModels/FullSailBrowserViewModel.cs`: persistent profile, asynchronous
  environment/control initialization, disposed guards after both awaits, navigation
  status and controls, initialization/action error reporting and idempotent disposal.
- `Windows/FullSailBrowserWindow.cs`: owned nonmodal windows, popup deferrals, child
  tracking, child/settings closure and model/view/host disposal on Closed.
- `MainWindow.FullSail.cs`: reuses the browser window and connects application grading
  actions. `App.cs` installs the framework COM cleanup policy and closes it on exit.
- `GuidedGrade.Tests/Services/FullSailFieldMapTests.cs` and
  `scripts/test-full-sail-fields.cjs` exercise mapping/script behavior. No test source
  reference to FullSailBrowserViewModel/Window or live WebView2 lifetime was found.

## Findings and ownership

| Priority | Finding | What must be established | Owner |
| --- | --- | --- | --- |
| High | Loaded awaits initialization, but the owner stores no completion task. Closed disposes synchronously; App shuts down on main-window close. Disposed checks prevent some late writes but do not demonstrate settled provider work before dispatcher shutdown. | Observe outstanding initialization/popup tasks; test close during environment creation and control initialization, including app exit. Define timeout/error policy without assuming the provider supports cancellation. | GuidedGrade lifecycle; framework acceptance reproduces the pattern |
| High | OpenChild is async-void, has no surrounding error handler or owner/child closed check after await, and assigns NewWindow after initialization. | Close parent/child during a pending request; ensure deferrals complete, no assignment targets a disposed child, and failures are observed. | GuidedGrade popup owner |
| High | Closed disposes settings, children, model, view and host sequentially. A throw can prevent subsequent cleanup. Browser.Dispose occurs before host detachment. | Detach and release according to explicit ownership; attempt remaining cleanup and report aggregate failures. Prove exactly-once release and no stale UI mutation. This ordering is not yet a reproduced WebView2 crash. | GuidedGrade owner; not an automatic framework disposer |
| Medium | Each child calls CreateAsync for an environment using the same profile path; the opener environment is not explicitly passed. | Verify the child satisfies WebView2's same-environment NewWindow requirement; prefer explicit sharing where appropriate. Same profile path alone is not acceptance evidence. No failure was reproduced here. | GuidedGrade provider adapter |
| Medium | Core event handlers are mostly anonymous and are not explicitly removed; disposal relies on Browser.Dispose. ProcessFailed and WindowCloseRequested handling is absent. | Verify subscriptions/retained references collect, and define process-failure and script-requested-close behavior. Lack of explicit unsubscription alone is not proof of a leak. | GuidedGrade provider policy |
| Acceptance | No real-browser retention, focus, popup, resize/airspace or repeated-close resource evidence was found. Existing framework sample uses WPF controls, not WebView2. | Add a deterministic browser acceptance workload with isolated profile and local HTML, exact package/runtime provenance, and observable outcomes. | FW-010 |

Microsoft documents that the target supplied for NewWindow must use the opener's
environment and must not have navigated. The application already suppresses home
navigation for child initialization; environment compatibility still needs evidence.
[Microsoft NewWindow contract](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/win32/icorewebview2newwindowrequestedeventargs?view=webview2-1.0.3800.47).

## Concrete FW-010 acceptance plan

1. Use the existing native host with a small WebView2 adapter and local test pages;
   isolate the test profile from GuidedGrade cookies and accounts. Record SDK,
   WebView2 runtime, framework package/source and machine details.
2. Assert stable browser identity through framework refresh/theme changes; focus,
   keyboard navigation, sizing, clipping and the supported overlay boundary. Do not
   promise WPF overlays over native content without a real test.
3. Exercise success/failure and close during each initialization phase, popup
   initialization and pending browser operations. Observe all tasks/deferrals;
   ensure no detached owner receives late updates and remaining cleanup runs on error.
4. Verify child environment compatibility, parent/child close races, navigation
   failure, process failure and script-requested close with explicit app policy.
5. Repeat mount/refresh/close while measuring retained hosts/adapters, native
   resources and browser process memory separately. Do not assume browser processes
   exit immediately or apply the managed-only memory probe as a browser budget.
6. If a failure reproduces in the framework host, compare the same adapter in plain
   WPF to distinguish host defects from application/provider behavior. Add a framework
   API only when a reproduced limitation requires it; preserve the full-list and
   themed performance gate for any runtime fix.

Application SSO, live grading transfers, taught field selectors and submission
semantics remain GuidedGrade work, not FW-010 release criteria. This review does not
authorize live grading actions or change the application.

## Verification

Documentation/source-review change only: no application or framework runtime code
changed and no new browser tests or performance measurements were run. The existing
framework commit `64bd242` passed hosted Windows/package/API, graph and 32/32
performance gates; that evidence does not establish WebView2 acceptance.
See [FW-010](backlog.md), [native contract](native-interop.md) and
[controlled WPF lifetime evidence](native-window-memory-2026-10-29.md).

## Subsequent acceptance work

The [first real-browser test slice](webview2-acceptance-2026-10-29.md) now passes
initialization and owned-popup close cases in a separate framework sample. It does
not change the GuidedGrade source reviewed above or close its app-side findings.
