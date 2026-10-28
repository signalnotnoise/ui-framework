# Virtualized lists and regression tests

`UI.VirtualList(rows, height)` requires a finite positive viewport height and unique, nonempty row keys. In the stress lab, enable **Virtualized list** to compare against the default full-list mode.

The WPF backend uses a VirtualizingStackPanel with pixel scrolling and a half-page cache on each side. Variable heights are measured by WPF. View descriptions and keyed row metadata exist for the whole list; native row controls and subscriptions exist only for realized rows. Visited rows retain logical component instances in snapshots until their keys leave the supplied list. Filtering a key out counts as removal.

Collection updates use individual insert, remove and move notifications. They no longer reset ItemsSource or dispose every visible host. WPF may recycle the moved row itself; unaffected rows retain controls and text selection. A large shuffle still performs multiple moves and may have quadratic collection-edit cost. This change does not claim constant-time reordering or a measured frame-rate improvement.

On recycling, subscriptions detach and OnUnmounted runs. On return, matching component instances and nested state restore, new native controls are created, and OnMounted runs again. Hooks must support repeated mount/unmount cycles. Resources disposed by OnUnmounted must be recreated by OnMounted. Removing a key or disposing the list releases its saved state. Component fields should not retain native controls.

Focused rows capture a visual path to the actual focused control and TextBox selection. Regeneration follows stable framework keys when available, so keyed editors can reorder. Unkeyed and native segments use visual position and control type; their structure must remain stable. A missing key or changed type skips restoration. Pending focus callbacks are canceled when a presenter is released and cannot target a reused row. Restoration only accepts empty/window focus or the original focused element, held weakly; it respects another editor selected before or after reattachment. Rows use standard WPF controls, so keyboard navigation and baseline screen-reader semantics come from those controls; comprehensive UI Automation coverage remains FW-006.

## Active editing and composition policy

Moving outside the viewport does not itself promise native-control destruction. WPF may retain a focused container. While the original control remains attached, editing and composition continue on that same control and item. The framework does not force focused-row eviction or turn off input methods to increase recycling.

Before a row is deactivated or released for recycling, regeneration, removal or list disposal, its nearest presenter ends keyboard focus on an actively composing editor while that editor and binding remain attached. WPF's normal `TextEditor` focus-loss path finalizes TSF/IMM input. The framework does not call a stored `FrameworkTextComposition.Complete()` afterward: that API only flags the current native event. Generic composition providers retain a virtual `Complete()` fallback if focus loss has not already finished them. End events are matched by editor source, because WPF creates different objects for native composition phases. A completed composition is not completed again. A completion failure is reported after row cleanup, so other rows still release.

Containing virtual lists do not also own an inner editor's session. Descendant editors deactivate before final state/selection snapshots are captured. The provider decides the final result, including an empty/canceled result; the framework does not synthesize candidate text or serialize an IME session. Schedule structural changes from native composition callbacks through the dispatcher, so WPF can finish the current event before removing its editor; synchronous removal during that event is outside this acceptance scope.

After completion, recycling saves logical state and the resulting selection. Returning rows receive new native editors. Removed keys discard saved state and focus; reinsertion starts a fresh row lifetime. Detached framework editors ignore late text changes and routed text input rather than applying it to either a former or newly realized item. Applications must likewise detach event handlers and cancel asynchronous work for native islands they own. An HWND-based island whose input does not route through WPF owns its own composition lifecycle.

These are framework lifecycle guarantees. Physical IME candidate windows, TSF-provider behavior and language-specific commit/cancel interactions need the separate visible-window acceptance below. Programmatic routed composition tests do not establish that acceptance.

## Verification

Run `dotnet test "tests/UI Framework.Checks"`. The suite discovers the current regression checks; use the generated TRX for current counts and results. Historical counts in dated evidence describe earlier revisions.

Coverage includes primitive edits, current callbacks, duplicate keys, conditional subscriptions, disposal, bounded mounting with 1,000 rows, offscreen insertion/removal/movement, unaffected control retention during a visible move, correct binding after movement, expanded-height scrolling, offscreen observer release, state restoration, fresh state after removal/reinsertion, and keyboard focus retention in a real WPF window. Forced container regeneration requires a new native input, balanced unmount/remount counts, retained component identity, restored keyboard focus, and correct subsequent edits. Actual scrolling separately requires native editor recycling after focus leaves the row, bounded realization, correct new-row edits, and rejection of changes sent to the detached old editor.

`VirtualizedEditingTests` exercises routed composition start, updates, commit and empty-result cancellation in visible WPF windows, including forced regeneration, removal, disposal, focused scrolling/reorder and provider failure. Added native-shape checks use different event objects per phase and providers that finish from focus loss, including nested selection capture. Focus tests cover an outside editor selected both before reattachment and after a restoration callback is queued, plus removal/reinsertion while restoration is pending. These tests use public `TextComposition` subclasses and routed events; they do not emulate native TSF.

Most other tests arrange WPF controls offscreen. The suite does not establish physical IME, accessibility, frame-rate or whole-application acceptance. Historical 90-check/21-stress-assertion results in performance-results.md predate this change. The three original suites remain grouped in Test Explorer; new checks are individually discoverable.

### Physical IME acceptance still required

Run the dedicated acceptance window from the repository root:

```powershell
dotnet run --project tools/VirtualizedEditingAcceptance -c Release
```

Choose an action, then focus **row 0** and begin/update composition within five
seconds. The queued action runs without another click, allowing regeneration,
removal or disposal while the editor still has focus. Reset between cases.
The window also offers delayed scroll and reorder actions. Its local JSONL log
records the startup input language, routed composition events and the item ID of every
binding write under `artifacts/fw005/manual-ime-*.jsonl`. Record candidate-window,
selection and focus observations alongside that log; events alone are not a
physical acceptance result. Note subsequent input-language switches in your
observations. `-- --smoke` checks only the window/binding setup
and regeneration, then closes automatically; it never counts as an IME pass.
This tool builds the current project references and records core/WPF assembly
hashes. It does not by itself validate installation from published packages.

Use the intended release-candidate packages on Windows with an actual Japanese or Chinese IME. Record OS/runtime/package versions and input language. In an editable 1,000-row list: start composition and change candidates; scroll away/return; reorder the edited row; force regeneration; filter/remove its key; and close the host. Repeat with explicit commit and Escape cancellation. Verify the final text belongs only to the original item, no candidates survive against another row, selection/focus follow the policy above, later input reaches the newly focused control, and shutdown releases the editor. Record observed provider behavior and failures; do not mark native IME acceptance passed from routed tests alone.
