# FW-010 WebView2 failure and recovery — October 29, 2026

The final focused campaign passes **15/15 checks**: three repetitions of five
cases, each in a fresh process with isolated test resources. Twelve are real
provider-failure/recovery cases; three verify application configuration preflight.
The existing five lifetime cases are rerun separately against the final sample.
FW-010 remains in progress; this is not full provider timeout or memory acceptance.

## Cases and acceptance

| Case | Required failure | Required recovery |
| --- | --- | --- |
| Profile preflight | A new file occupies this test's profile directory; IOException is reported before environment initialization starts | Failed host releases once and reports its error; fresh owner loads local page with a valid profile |
| Missing runtime | Real WebView2RuntimeNotFoundException from an explicitly nonexistent runtime folder | Failed owner releases; fresh owner uses the installed runtime and executes local-page script |
| Missing local page | Matching NavigationCompleted reports IsSuccess=false (this runtime reports WebErrorStatus=Unknown) | A valid local page loads and scripts execute on the same controller |
| Renderer termination | Real RenderProcessExited event and one recorded diagnostic | Valid page/script work again on the same controller and browser PID; owner releases once |
| Browser termination | Real BrowserProcessExited failure plus environment BrowserProcessExited notification for the test PID | Wait for exit, release failed owner, recreate with the same isolated profile and a new browser PID; real local-page script and framework refresh succeed |

All final cases report dispatcher shutdown complete and framework cleanup policy
closed. Expected failures remain in logs and result fields. Assertions require the
specific expected diagnostic and reject extra errors; no retry loop turns a failed
case into a success. Recovery is explicit sample/application policy, not automatic
framework error suppression or preservation of arbitrary in-page state.

## Defects found and corrected in the sample owner

1. **Crash cleanup queried an invalid control.** WebView2's CoreWebView2 getter
   throws after browser-process failure. The owner now caches the core object when
   subscribing, removes handlers through that reference, clears the reference and
   releases the host. Cleanup attempts remaining steps and surfaces accumulated
   errors, instead of allowing one failed unsubscribe to skip disposal.
2. **Invalid profile configuration could stall the provider.** A file occupying the
   profile path returned a COM error in pilots, but exceeded the 30-second await
   limit in the first repeated campaign. The sample now rejects this known invalid
   input before calling the provider. This prevents that configuration from entering
   provider startup; it does not establish recovery from arbitrary provider hangs.
   A separate missing-runtime case retains genuine provider initialization-failure
   coverage. Both old outcomes are preserved, with the workload change explicit.

The timeout occurred while the full regression run was also active. These data do
not establish whether load affected it; no claim of a timing root cause is made.
No timeout was extended and no passing result is assigned to that earlier run.

## Fault injection and other pilot findings

Process termination uses only GetProcessInfos from the test's environment. The
helper validates its exact isolated UserDataFolder, requires one process of the
requested kind, checks the executable name, and cross-checks the browser PID with
the controller. It retains the process handle and terminates that process only.
It does not enumerate or kill personal browsers by name, or terminate a process tree.

An earlier DevTools Page.crash probe delivered the process-failure event but left
the command task pending until the 30-second test limit. That failure is retained.
The final process fault method avoids introducing that unfinishable command into
the ownership test. Another pilot incorrectly required a specific navigation error
enum; the real failed local-file navigation returns Unknown. The corrected check
still requires a failed navigation with the matching navigation ID, followed by a
successful real-page recovery. A transient compile error is also retained.

These policies follow the distinction between renderer recovery and browser
recreation described in [Microsoft's process-event guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/process-related-events).
The test waits for [BrowserProcessExited](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environment.browserprocessexited?view=webview2-dotnet-1.0.4022.49)
before recreating the browser; it does not assume the process is gone merely because
the control reported a failure.

## Reproduction and evidence

PowerShell 7, installed WebView2 Runtime, fresh output directory:

```powershell
./tools/Test-WebViewAcceptance.ps1 -Modes initialization-recovery,runtime-missing-recovery,navigation-recovery,renderer-recovery,browser-recovery -OutputDirectory artifacts/webview-recovery-new-run
```

These explicit modes intentionally fail test resources; they are not in the default
runner. No account or grading site is involved. The user authorized desktop
interaction for this continuation, but these cases required no keystroke injection
or foreground activation. Keyboard/airspace cases remain separately opt-in.

Recorded SDK/runtime: .NET 10.0.402/10.0.13, WebView2 SDK 1.0.4314.35 and browser
156.0.4314.50. Base framework revision is `64bd242`; measured sample/tooling source
is uncommitted and frozen with hashes. [Preserved evidence](verification-evidence/2026-10-29-webview2-recovery/README.md)
includes pilot failures, final reports and source. Browser profiles/crash/cache
contents are retained locally but excluded from the archive.

The full release regression run during this work passes 172 tests, 15 visual checks,
21 stress assertions, startup/disposal and controlled native-window checks. The
subsequent preflight/missing-runtime sample additions are covered by their final
build and repeated browser cases. Core/WPF runtime and GuidedGrade source are
unchanged. No new performance comparison is claimed or needed for this sample/test
change; existing hosted performance evidence belongs to `64bd242`.

## Remaining FW-010 work

Generic stalled-provider timeout/shutdown policy; script-requested window close;
pointer/clipping and broader/repeated interaction acceptance; browser resource and
process retention; and GuidedGrade's adoption of the demonstrated ownership and
recovery policies. The framework host supported these cases without a runtime API
change. Real local-page recovery does not establish SSO/session restoration or
safe replay of grading submissions.
