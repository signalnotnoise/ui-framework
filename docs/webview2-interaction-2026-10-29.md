# FW-010 WebView2 keyboard and airspace pilot — October 29, 2026

Two real-browser interaction cases passed once each on WebView2 runtime
156.0.4314.50 / SDK 1.0.4314.35, .NET runtime 10.0.13. This is a bounded local
pilot, not repeated or hosted acceptance. FW-010 remains in progress.

## Keyboard and focus

The local interaction page exposes two inputs and a button. WPF buttons immediately
before and after the framework host provide traversal endpoints. The test verifies:

- Actual Win32 input travels from WPF into the browser, between HTML controls and
  back to WPF, in both Tab and Shift+Tab directions, without trapping focus.
- Ctrl+A and Unicode input replace the first input's value; Enter activates the
  HTML button once. Browser key events report `isTrusted`; no synthetic DOM keyboard
  event or script-based focus change substitutes for those key presses.
- Framework refresh and one scoped theme application preserve the browser instance,
  DOM focus, edited value and selection.

The harness checks the foreground HWND before each input batch and fails if focus
has moved. It also rejects input when a physical modifier is held. These guards
reduce accidental input risk; the test still deliberately takes desktop focus and
sends keys, so it now requires explicit interactive opt-in.

## Airspace and sizing

The real browser viewport follows window resizing. Screen pixel checks and cropped
screenshots establish a specific boundary:

| Setup | Observed result |
| --- | --- |
| Magenta WPF sibling overlay above the host at ZIndex 100 | Native browser remains visible over it; sampled pixel is browser blue (#0060C0) |
| Same setup with WebView2 hidden | Magenta overlay becomes visible (#FF00FF), confirming it exists and is correctly positioned |
| Browser restored, magenta WPF Popup in its separate HWND | Popup is visible above the browser |

The screenshots were visually inspected. The hidden-browser control check avoids
mistaking an absent overlay for an airspace limitation. These are actual screen
captures, not WPF RenderTargetBitmap output that omits native window content.
Only test-window bounds are captured; profiles use the existing isolated local
test setup. This verifies visibility, not mouse routing or full clipping semantics.

This is the documented behavior of standard WPF WebView2's HwndHost integration,
not evidence of a framework Z-order bug. Microsoft's composition-based WebView2
control is an alternative for overlapping WPF UI, but it was not tested or adopted
here. [Microsoft WPF WebView2 airspace documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/platforms/wpf).

## Desktop interaction and evidence

The first run failed before sending keys because Windows had not activated the
test window. That failure is preserved. The harness was then changed to wait up
to 60 seconds for activation. The computer-use tool activated the uniquely selected
test window; the keyboard pilot passed, followed by the airspace pilot. No account,
GuidedGrade instance or remote grading page was used.

After the user questioned desktop use, further foreground testing stopped. No
repeat campaign was run. The runner now defaults to the original five lifetime
cases; interaction requires `-IncludeInteractive`. Direct executable invocation
also requires `--interactive` for these modes. Those opt-in changes were compiled
and the runner's rejection path checked without reopening test windows.

Only run this command when explicitly ready for focus changes, keystrokes and
test-window screenshots; avoid interacting with the desktop during the cases:

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes focus-keyboard,airspace -IncludeInteractive -OutputDirectory artifacts/webview-interactive-new-run
```

Use PowerShell 7. Profiles, reports and screenshots are retained; select a fresh
output folder. A desktop that cannot grant foreground activation cannot produce
a passing keyboard result. The x64 input layout is explicitly checked.

[Preserved pilot evidence and exact tested source](verification-evidence/2026-10-29-webview2-interaction/README.md).
The final source differs from the tested pilot only in the subsequent explicit
opt-in safeguards and documentation. No core/WPF runtime or GuidedGrade code changed.
No new performance comparison or full GUI regression run was performed in this
slice. Prior release/lifetime evidence remains linked in the
[initialization and popup report](webview2-acceptance-2026-10-29.md).

## Remaining work

Repeat keyboard/airspace cases with explicit permission and record supported
desktop/DPI conditions. Still open: pointer routing/clipping, additional focus
and theme scenarios, provider failure/recovery and script-close policy, browser
process/resource retention, and applying lifecycle improvements in GuidedGrade.
The passing native-airspace test records an unsupported sibling-overlay behavior;
it does not make that behavior supported or require changing the framework host.
