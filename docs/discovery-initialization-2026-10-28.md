# Virtual-row discovery failure diagnosis — October 28, 2026

Both failing tests stop at their first offscreen name lookup, before realization
or reorder/removal assertions. The data and name exist, but WPF returns no native
provider for the offscreen peer.

A standalone probe reproduced the ordering problem. The tests create managed
peers and recursively cache their children before a native UI Automation query
connects the window. Read-only diagnostics show window and list peer handles at
zero. A native AutomationElement query initializes the window handle, but cached
list peers can retain zero. WPF cannot attach offscreen item peers to that parent.
Its [TrySetParentInfo implementation](https://source.dot.net/PresentationCore/System/Windows/Automation/Peers/AutomationPeer.cs.html)
rejects a parent with a zero window handle.

The first root-only probe succeeded once, but repeats showed that query alone is
not sufficient after children are already cached. A descendant query also left
cached child handles at zero. With the native query followed by public
ResetChildrenCache/GetChildren traversal, three fresh-process runs showed:

- 1,000 logical rows; the last peer's name is Record 999.
- Before native query: window/list handles zero, discovery returns null.
- After native query: window handle nonzero, cached list handle still zero.
- After tree refresh: list has the window handle; discovery succeeds and exposes
  IVirtualizedItemProvider in all three runs.

The probe uses reflection only to read diagnostic state/name, never to write WPF
internals. The initialization uses a public UIA client on a background MTA thread
while the window dispatcher runs. No production runtime or test assertions were
changed. Original and unsuccessful probe logs remain alongside the final probe.

The required test correction is deterministic native-provider initialization
before building the peer tree, or refreshing that tree after initialization.
Keep the original discovery, bounded-realization and identity assertions and rerun
them: later assertions have not yet been validated by this diagnosis. An external
accessibility client may have initialized earlier passing windows, but the exact
cause of those historical passes was not captured. Do not infer a production
accessibility failure solely from this incorrectly initialized direct-peer test.

[Probe and raw logs](verification-evidence/2026-10-28-discovery-initialization/manifest.json).

## Implemented correction and verification — October 28, 2026

`TestAutomationTree` now performs the native descendant query on a background
MTA client while pumping the window dispatcher, then refreshes cached peers
using public APIs. The existing STA test runner bounds the entire operation.
Only the two discovery tests use this helper; no runtime code or assertions
were changed for this correction.

- Focused accessibility acceptance: 5/5 pass.
- Full framework suite: 146/146 pass, zero skipped; build has no warnings/errors.
- Both discovery tests repeated in two additional fresh test hosts: 2/2 each.
- Release validation: 15 visual checks, 21 stress assertions and actual-window
  startup/edit/disposal pass.
- Graph tooling: 25 tests pass; paired-performance and timing-analysis checks pass.
- API compatibility guards and local package installation/rendering/state/native
  interop smoke tests pass. Packages were not published.

The latest runtime performance evidence remains 32/32 passing. This test-only
correction does not require another benchmark. Physical contrast acceptance and
FW-005 physical IME acceptance remain open. Earlier failed logs are preserved.

[Correction sources, test results and SHA-256 manifest](verification-evidence/2026-10-28-discovery-test-setup/manifest.json).
