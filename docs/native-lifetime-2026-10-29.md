# Native ownership and lifetime verification — October 29, 2026

FW-010 and FW-018 are active P1 items, ahead of new-control work. This first slice
uses deterministic framework-owned test fixtures and the existing WpfUI.Native
callbacks. No core/WPF runtime, public API, benchmark workload or budget changed.

## Ownership boundaries

The application adapter owns initialization, cancellation, provider failures,
event subscriptions and provider resources. The framework owns retained identity,
detachment, original-release invocation and failed render cleanup. A factory
that throws before returning still owns its partially initialized resources;
the framework cannot release an element it never received. Accepted elements
are detached before release, including when initial update fails.

The test adapter explicitly observes its initialization task, marshals completion
back to the owning dispatcher, checks disposal before writing UI, and disposes a
resource returned after cancellation/removal. It reports initialization faults,
even after removal, rather than using async-void callbacks or swallowing failures.
Owner-requested cancellation is handled as cancellation. Removal and remount use
distinct adapter instances; a previous completion cannot write the replacement.

This is a bounded test adapter, not a production browser/editor wrapper or a new
async framework API. The dispatcher stays alive until initialization settles.
Dispatcher shutdown with uncooperative pending work, throwing cancellation
registrations, third-party popup/airspace rules and provider-specific resource
thread affinity remain separate work. A provider task can legitimately retain
its result; the collectability check keeps that task alive while requiring the
disposed host/control to become unreachable after completion.

## Focused evidence

Thirteen focused checks pass against unchanged framework runtime source:

- Retained native identity across initialization and updates; one-time release.
- Completion after removal and after host disposal; cancellation requested,
  late acquired resource disposed, detached text unchanged.
- Old completion after remount cannot overwrite the replacement island.
- Initialization faults observed before and after removal; expected cancellation.
- Partial initial update failure releases the accepted control and late resource.
- Factory failure cleans application-owned partial work and releases an already
  accepted sibling, without invoking release for the unreturned element.
- Twenty-four disposed hosts/islands and 32-KiB payloads become collectible with
  the external publisher still alive and a queued renderer refresh drained.
- Scrolling realizes additional virtual native rows; visited controls/payloads
  and workspace become collectible after disposal.
- A completed provider task does not retain a disposed async host/control.
- A deliberately missing application unsubscribe is detected; collection succeeds
  after explicit unsubscribe. This validates probe sensitivity.

Weak-reference factories are not inlined, and assertions run after local strong
references leave scope and queued dispatcher work drains. Forced collection is
verification, not production cleanup. Passing this bounded workload does not
establish a sustained memory budget or whole-application leak freedom. These
checks verify managed reachability, not native handle counts or provider-owned
unmanaged allocations.

The first run passed 9/10; one retained-identity test queried the visual tree
before applying layout. Applying layout fixes the setup; the identity assertion
remains unchanged. Initial failure evidence is retained. After adding cancellation,
remount and settled-task cases, all 13 focused checks pass.

Full Release validation passes: **172 tests, 15 visual checks, 21 stress
assertions and startup/editing/disposal**, with zero build warnings/errors.
[Preserved evidence and source](verification-evidence/2026-10-29-native-lifetime/README.md).
No performance comparison is needed for this
test/documentation-only slice; no new performance improvement is claimed.

## Remaining work

FW-010: framework-owned real-window integration example with focus, child popups,
resizing/overlay boundaries and explicit shutdown sequencing; use a reproduced
contract limitation before proposing any framework API. Application login,
navigation and grading integrations remain out of scope.

FW-018: sustained navigation/scrolling/visited-row state measurement and retained
memory budgets; cleanup failure and shutdown collectability; evaluate actionable
component/key diagnostics and measure overhead if runtime instrumentation is added.
Both items remain in progress. See [backlog](backlog.md).

## Follow-up completed the same day

The [real-window and sustained-memory phase](native-window-memory-2026-10-29.md)
now covers controlled shutdown, throwing cancellation callbacks, UI-thread native
buffer release and repeated memory observations. The remaining-work section above
records the first phase; consult the follow-up and backlog for current status.
