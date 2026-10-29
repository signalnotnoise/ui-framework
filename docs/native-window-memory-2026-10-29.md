# Real-window ownership and sustained memory — October 29, 2026

FW-010 and FW-018 remain in progress. This phase adds a real WPF window integration
sample and repeatable memory measurements without changing framework runtime,
public APIs, accepted performance baseline or budgets.

## Window and shutdown contract

`samples/NativeLifetime` exercises the existing native factory/update/release callbacks.
The application adapter owns cancellation, observed initialization, subscriptions,
popup cleanup and explicit unmanaged buffer disposal on its UI thread. The window
owner defers closing, disposes/detaches the host, drains initialization, closes the
window, then shuts down the dispatcher and application cleanup policy.

Real-window checks pass for keyboard focus and selection across retained updates,
popup HWND creation/release, resizing and a managed WPF overlay hit test. Shutdown
cases cover late success, late fault, cooperative cancellation, a throwing cancellation
callback, and timeout followed by retry. Cleanup errors are reported while remaining
cleanup continues. A timeout keeps the window/dispatcher alive; it is not successful
cleanup of an uncooperative provider. No new framework API was needed for these cases.

This is a controlled WPF adapter, not WebView2 or HwndHost acceptance. Native airspace,
provider-specific shutdown, browser popups and login/SSO remain unverified. The close
coordinator is sample application policy, not a general production shutdown service.

## Repeated memory evidence

Three fresh processes each ran 3 warmup and 60 measured batches, with 4 workspace
cycles per batch and 500 keyed rows per workspace: **756 cycles and 57,456 native
mounts including warmup**. Each cycle edits, scrolls, navigates away/back, verifies
retained text, queues a publisher update and disposes the workspace. The external
publisher remains alive. Each native editor owns a 32-KiB unmanaged buffer and
16-KiB managed payload; logical row state owns an 8-KiB payload.

After every batch, tracked weak references have zero survivors, created/released
counts balance, owned native bytes return to zero and no wrong-thread release is
observed. A deliberately rooted disposed editor is detected by the negative control.
All processes report completed dispatcher shutdown and cleanup policy closure.

| Soak repetition | Post-GC managed change | Private bytes change | Working set change | Handle change |
| --- | ---: | ---: | ---: | ---: |
| 1 | +76,856 B | -4,263,936 B | +7,356,416 B | +2 |
| 2 | +77,168 B | +4,747,264 B | +12,681,216 B | +2 |
| 3 | +25,344 B | -1,327,104 B | +6,340,608 B | +1 |

GDI and USER counts have zero first-to-last change in all three repetitions.
These process trends are observations, not an approved memory budget or proof of
whole-process leak freedom. Forced GC tests reachability; it is not an application
cleanup recommendation. Report bookkeeping grows during the run and process/runtime
memory can remain resident; these measurements do not attribute the remaining growth.
Natural heap measurements, full ranges and regression slopes are retained in raw JSON.
The earlier shorter three-process campaign is preserved, not replaced by this soak.

## Retained state while a list is open

The separate checkpoint workload visits every row, then removes keys:

| Checkpoint | Logical components | Native editors | Logical payload | Owned native bytes |
| --- | ---: | ---: | ---: | ---: |
| Initial viewport | 10 | 10 | 81,920 B | 327,680 B |
| All 500 rows visited | 500 | 10 | 4,096,000 B | 327,680 B |
| Half the keys removed | 250 | 10 | 2,048,000 B | 327,680 B |
| All keys removed | 0 | 0 | 0 B | 0 B |
| Host disposed | 0 | 0 | 0 B | 0 B |

Virtualization bounds realized native controls, while logical state intentionally
retains visited existing keys. This is distinct from O(item-count) descriptions.
Removing keys releases their retained state; disposal releases all observed objects.

## Reproduction and remaining acceptance

Use PowerShell 7 and the same .NET SDK/runtime as the recorded manifest (10.0.402 /
10.0.13). The run opens real windows and checks keyboard focus; avoid interacting
with those windows during execution. Choose a fresh output directory:

```powershell
pwsh -NoProfile -File tools/Test-NativeLifetime.ps1 -Batches 60 -OutputDirectory artifacts/native-lifetime-new-run
```

The analyzer rejects incomplete work, missing ownership probes, survivors, native
resource imbalance and incomplete shutdown. Its regression fixtures run in CI.
`tools/Test-Release.ps1` now includes the real-window ownership/shutdown checks.
Full Release validation passed **172 tests, 15 visual checks, 21 stress assertions,
startup/editing/disposal and real-window ownership/shutdown**, with no build warnings
or errors. Analyzer regression fixtures pass.
These are correctness/memory observations, not replacements for the full-list and
virtualization performance comparison. No latency improvement is claimed.

FW-010 still needs provider-specific/native-airspace acceptance or an explicit v1
scope decision. FW-018 still needs representative memory budgets and a decision on
actionable component/key diagnostics; instrumentation would require overhead checks.
See [backlog](backlog.md), [first phase](native-lifetime-2026-10-29.md), and
[preserved raw evidence and source](verification-evidence/2026-10-29-native-window-memory/README.md).
