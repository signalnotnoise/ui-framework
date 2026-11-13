# Native provider stalls: v1 ownership contract

FW-010 scope decision, November 11, 2026.

The framework guarantees its synchronous native-host ownership contract: retain by
declared identity, detach before release, and invoke the accepted release callback
once. Applications own asynchronous provider tasks, cancellation, deadlines and
error presentation. WebView2 is accepted through the documented sample adapter;
there is no framework promise that a third-party initialization call terminates.

## Deadline and close behavior

1. A deadline bounds the caller wait, not the provider lifetime. Report a visible
   initialization/close failure and retain the pending operation and native owner.
   Do not turn a timeout into a successful initialization or shutdown result.
2. Mark the owner closing. Reject further initialization on it and prevent any
   late completion from attaching a controller or assigning a popup to that owner.
3. Preserve one close/drain task. Keep its dispatcher alive while work is pending;
   await and report late faults. When the operation settles, detach and release once.
   Retry initialization only with a fresh owner after drain completes.
4. If it never settles, close remains pending indefinitely. The application should
   retain a responsive failure/status surface where possible, avoid accumulating
   retries, and offer waiting or an explicit whole-application exit/restart policy.
   Forced process exit is not graceful disposal and may lose unsaved state. The
   framework does not perform it or claim cleanup completed. A blocked dispatcher
   also prevents in-process timeout UI from being dependable.
5. An application requiring a hard shutdown deadline must place the provider in a
   separately owned process with an explicit termination/state-recovery design.
   Such process isolation is outside the v1 native-host API and this acceptance.
   Killing arbitrary/shared WebView2 processes is not a supported cancellation path.

The sample's StartWithin/CloseWithin and late-success/fault checks demonstrate the
bounded-wait and retained-ownership policy with controlled pending tasks. They do
not claim to reproduce or recover an actual permanently hung provider. This is an
explicit support limit, not an unimplemented promise blocking FW-010.

## Browser interaction scope

Accepted: standard WPF WebView2 in a rectangular allocated region; local-page
keyboard and pointer routing, scrolling, resize, focus/text/selection retention
through framework refresh and three theme changes; owned popup lifetime and
separate popup HWND presentation. Browser document colors remain page-owned.

Not promised: WPF sibling overlays above the standard browser HWND, arbitrary WPF
Clip/opacity/transforms on browser pixels, composition-controller behavior, arbitrary
website/SSO behavior, or a general browser memory budget. The airspace tests record
the overlay limitation and verify the hidden-browser and separate-popup controls.
Browser process/resource measurements cover the defined local-page workload.

GuidedGrade must adopt its own ownership/recovery policies. Framework acceptance
does not certify the application's current browser shutdown code. Representative
memory budgets and diagnostic API work continue under FW-018.
