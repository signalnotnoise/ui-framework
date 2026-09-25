# Application-owned COM cleanup prototype

The [native wait investigation](editor-com-cleanup-2026-09-25.md) motivated an application-owned cleanup policy. The implementation is **experimental sample code**, outside both package APIs and the consumer application. It does not alter the release workload, accepted baseline or budgets, and does not clear NuGet publication.

## Ownership and scheduling

`samples/ComCleanupLifecycle/ApplicationComCleanupPolicy.cs` installs once on an application-owned STA. The underlying CLR eager-cleanup setting cannot be reversed on that thread. Dispatcher operation completion requests cleanup at ContextIdle; the cleanup operation does not reschedule itself. Multiple requests coalesce. `RunUpdate` marks explicit application update boundaries, including synchronous updates that might enter a nested message loop.

Cleanup is deferred during marked updates. Reentrant cleanup requests do not recursively enter cleanup. A cleanup failure is observable through `LastError`; later requests can retry. The owner must close after disposing its native hosts and before dispatcher shutdown. Closing during an update or cleanup callback is rejected. Successful close aborts pending work and detaches the dispatcher hook. Failed shutdown cleanup throws and leaves the owner open for retry. Close does not re-enable automatic CLR cleanup.

This prototype deliberately does not install itself from a ViewHost or TextEditor. A production application would need an explicit owner, an error-reporting path, and shutdown ordering. `LastError` polling in this sample is not a complete application error-reporting design.

## Lifecycle verification

`tools/Test-ComCleanupLifecycle.ps1` builds the isolated WPF check program and a small native IUnknown fixture using Visual Studio C++ build tools. The fixture counts live objects and records destruction off the creating thread. The process has a 60-second test deadline.

The final run passed **49 assertions**, including:

- Application-update exceptions release the boundary so cleanup can continue.
- A nested dispatcher frame cannot run cleanup inside a marked update.
- Reentrant requests coalesce without recursively entering cleanup.
- Calls from another thread are rejected.
- Injected cleanup failure is observable and recoverable; failed shutdown is retryable.
- A framework native TextBox retains its identity, text and selection across refresh/cleanup cycles and is released once after detachment.
- Ten rounds of 100 COM wrappers, then 100 more at shutdown, leave **zero of 1,100 fixture objects alive**, all destroyed on their owning STA. Weak references to their managed wrappers clear.
- No queued cleanup runs after close.

Forced GC/finalizer draining is used only in these lifecycle checks, not in the timing workload. This tests the fixture's ownership and wrapper lifetime; it is not proof that all WPF/third-party COM resources are leak-free. InputMethod remains enabled, but actual IME composition and screen-reader behavior were not exercised.

## Timing

`EditorWaitDiagnostics` links the exact prototype source and adds `application-policy` mode. It retains the ordinary native workload: 1,000 editors, 50 updates, 10,000 checked text replacements, original strings, input-method support, and read-only/undo toggling. Three fresh-process pairs alternate mode order. No warmup processes are discarded. Final cleanup is included in update elapsed time and allocation. The diagnostic closes its policy while the timed editor graph is still present; the separate lifecycle test verifies host-disposal-before-close ordering.

| Metric | Normal median | Prototype median | Change |
| --- | ---: | ---: | ---: |
| Update elapsed time | 21,277.50 ms | 6,872.25 ms | -67.70% |
| UI-thread update allocation | 819,705,648 B | 821,639,208 B | +0.24% |

Normal update totals range from 19,884.84 to 21,403.04 ms; prototype totals range from 6,762.80 to 7,094.45 ms. Final-step time falls from 13.01–13.83 seconds to 0.17–0.19 seconds. All six processes completed workload assertions. [Raw timing and lifecycle evidence](performance-evidence/2026-09-25-application-cleanup-policy/summary.json) includes the exact diagnostic and policy source snapshot.

These are native diagnostic results. They do not measure mount cost, framework component work, or actual consumer responsiveness, and they are not a substitute for the full-list, virtualized, themed and editor release comparisons using `tools/Test-Performance.ps1`.

## Reproduce

```powershell
./tools/Test-ComCleanupLifecycle.ps1 -OutputDirectory artifacts/cleanup-lifecycle-new
./tools/Test-EditorWaitDiagnostics.ps1 -Modes normal,application-policy -Repetitions 3 -OutputDirectory artifacts/cleanup-timing-new
```

Remaining adoption checks: actual framework and consumer comparisons under an explicitly declared policy, longer WPF/third-party COM lifetime stress, visible typing/IME/accessibility, and application error/shutdown integration. Keep the original failed gate evidence when evaluating this mitigation.

## Opt-in framework experiment

`tools/Test-Performance.ps1 -IncludeLayoutEditors -ExperimentalComCleanup -ReportOnly`
also measures the real framework editor tree. The switch is experimental and is
absent from both release workflows. It does not change the accepted revision or
budgets. The script copies the exact policy source into the archived baseline
harness and validates the policy identifier in each editor report. Full-list,
virtualized and themed comparisons continue to run with their ordinary workload.

Both editor revisions install the policy inside mount timing. Update timing
includes detaching and disposing the native host and framework host, followed by
the final cleanup before dispatcher shutdown. Normal editor mode retains its
existing measurement boundaries. Consequently experimental editor timings must
not be presented as an unchanged release-gate result or compared directly to
normal editor timing as a precise estimate of the policy's speedup.

Experimental summaries explicitly set `experimentalComCleanup: true` and
`releaseEligible: false`, even if every numeric budget passes. Passing establishes
only that the current framework meets those budgets against the accepted revision
under this explicit application policy; it does not establish production adoption
or validate interactive input and accessibility.

### Framework results

The September 25 experiment used SDK 10.0.401/runtime 10.0.12 on this Windows
machine, accepted revision `78c88fb`, three alternating measured processes per
side/scenario and one discarded warmup per side/scenario. All 32 processes
completed, including all eight editor processes. The policy source SHA-256 was
identical in both harnesses. No workload, baseline or budget was reduced.

| Scenario | Mount time | Update time | Mount allocation | Update allocation |
| --- | ---: | ---: | ---: | ---: |
| Full list | +5.60% | -4.96% | -1.00% | -4.96% |
| Virtualized | +9.84% | +5.94% | -1.61% | -5.12% |
| Themed full list | +2.03% | **+12.97%** | -2.67% | +6.42% |
| Editors, experimental policy | +0.54% | -3.50% | +0.13% | -3.30% |

Editor mount medians were 2,223.91/2,235.86 ms (baseline/candidate), with
126,187,600/126,355,792 allocated bytes. Editor update medians were
8,166.98/7,881.03 ms with 1,161,578,200/1,123,242,552 allocated bytes. Both built
once on mount and 50 times during updates. Component work matched in every
scenario. This compares framework revisions under the policy, not the policy's
effect against default cleanup.

**Overall result: failed, 31 of 32 metric budgets met.** Themed update time exceeded
its unchanged 12% limit. Its three individual paired increases were approximately
13.22%, 12.34% and 12.02%; candidate times ranged from 11.33–11.63 seconds, versus
10.12–10.33 seconds for the baseline. This is a consistent measured slowdown in
this short campaign, not evidence to dismiss as an isolated outlier. The policy
is disabled in that scenario, so this experiment does not attribute the themed
slowdown to COM cleanup. Earlier presenter/theme investigation remains relevant;
further themed profiling and a full seven-sample validation remain necessary.
Virtualized mount timing also sits close to its 10% limit.

[Raw reports, logs, summary and exact harness snapshots](performance-evidence/2026-09-25-framework-cleanup-experiment/summary.md)
preserve the failure. The initial sandbox attempt failed during NuGet configuration
access before any measurements; the completed run used normal desktop access.
The 15 visual stress checks passed after measurement. A separate candidate
smoke check with default cleanup (no experimental switch) then exceeded the
unchanged 120-second limit, produced no report, and was terminated. Its
`default-smoke-failure.json` is preserved with the evidence. This confirms that
the ordinary editor failure remains unresolved; this single smoke run is not a
paired estimate of policy speedup. Production adoption, real
typing/IME/accessibility checks and the ordinary release gate are still pending.
