# FW-010 final acceptance — November 11, 2026

Three fresh-process repetitions of each final interactive mode pass **9/9**, after
a 3/3 pilot. SDK 1.0.4314.35, installed WebView2 runtime 157.0.4341.42, .NET 10.0.13.
The user explicitly authorized completing interactive acceptance. Only the exact
framework test window was activated/operated; local pages and isolated profiles were
used. Keyboard injection is foreground-guarded; pointer input came through the
computer-use tool using freshly observed test-window screenshots.

## Observed outcomes

- Forward/reverse native-WPF/browser Tab traversal, text entry and Enter activation
  pass in all three repetitions. Refresh and three theme applications retain the
  core object, DOM focus, text and selection. Trusted keyboard events are recorded.
- Resize updates the browser viewport. Browser pixels cover a higher-Z WPF sibling
  overlay; hiding the browser reveals that overlay. A separate popup HWND appears
  above the browser. These are verified standard-host limitations/control cases.
- Pointer cases verify one WPF button click, one trusted HTML button pointer event,
  HTML focus and a positive scroll offset. Pixel probes sample blue inside and white
  in the left margin outside the browser's allocated rectangle. Screenshots preserve
  the layout. This is a representative rectangular-boundary check, not arbitrary
  geometry/transform/composition-control acceptance.
- Every case releases once and reports dispatcher shutdown and cleanup-policy close.

The pilot's initial accessibility-index click could not resolve input geometry;
we re-observed the test window and used screenshot coordinates. After a successful
scroll, one refresh found the already-closed test window. The application report
confirmed completion; no test assertion or timeout was relaxed and no failed test
was discarded. The three repeated runs passed without a test retry.

## Permanent-stall decision

The [v1 policy](native-provider-stall-policy.md) makes timeout/report/retain/drain the
supported in-process behavior. A deadline does not cancel third-party work. Close
remains pending if the provider never settles; no forced disposal or successful
shutdown claim is permitted. Hard shutdown deadlines require application-owned
process isolation. Whole-application forced exit/restart is an explicit app policy,
not graceful framework cleanup. A blocked dispatcher is outside an in-process
responsiveness guarantee. This deliberately limited contract resolves the scope
question; it does not claim that a synthetic pending-task test reproduced a hang.

## Prior evidence retained

- [Ownership and real-browser lifetime](webview2-acceptance-2026-10-29.md).
- [Failure and recovery](webview2-recovery-2026-10-29.md).
- [Bounded waits and script-close](webview2-shutdown-2026-11-11.md).
- [Resource retention and exception-test review](webview2-retention-2026-11-11.md):
  270 parent/popup cycles, 540 one-time releases, 1,350 captured process exits and
  2,970 collected weak references, including calibrated negative controls.

Full Release validation passes 172 framework tests, 15 visual checks, 21 stress
assertions and actual-window startup/editing/disposal and native lifetime checks. [Final evidence archive](verification-evidence/2026-11-11-webview2-final/README.md)
preserves raw reports, screenshots and the tested source manifest/snapshot.
No core/WPF runtime or GuidedGrade code changed, and no new performance comparison
or hosted release gate is claimed. The 9 interactive cases are an explicit opt-in
acceptance command, not unattended CI.

FW-010 is complete within this documented scope. Final regression and both
knowledge graph checks pass. FW-018 retains representative memory budgets/diagnostics;
GuidedGrade must adopt its app-owned lifecycle policy separately. Arbitrary WPF
clips/overlays, composition-controller support, arbitrary websites/SSO and forced
provider termination are not part of this acceptance contract.
