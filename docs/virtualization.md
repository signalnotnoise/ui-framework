# Virtualized lists and regression tests

`UI.VirtualList(rows, height)` requires a finite positive viewport height and unique, nonempty row keys. In the stress lab, enable **Virtualized list** to compare against the default full-list mode.

The WPF backend uses a VirtualizingStackPanel with pixel scrolling and a half-page cache on each side. Variable heights are measured by WPF. View descriptions and keyed row metadata exist for the whole list; native row controls and subscriptions exist only for realized rows. Visited rows retain logical component instances in snapshots until their keys leave the supplied list. Filtering a key out counts as removal.

Collection updates use individual insert, remove and move notifications. They no longer reset ItemsSource or dispose every visible host. WPF may recycle the moved row itself; unaffected rows retain controls and text selection. A large shuffle still performs multiple moves and may have quadratic collection-edit cost. This change does not claim constant-time reordering or a measured frame-rate improvement.

On recycling, subscriptions detach and OnUnmounted runs. On return, matching component instances and nested state restore, new native controls are created, and OnMounted runs again. Hooks must support repeated mount/unmount cycles. Resources disposed by OnUnmounted must be recreated by OnMounted. Removing a key or disposing the list releases its saved state. Component fields should not retain native controls.

Focused rows capture a visual path to the actual focused control and TextBox selection. Regeneration follows stable framework keys when available, so keyed editors can reorder. Unkeyed and native segments use visual position and control type; their structure must remain stable. A missing key or changed type skips restoration. IME composition is not captured. Pending focus callbacks are canceled when a presenter is released and cannot target a reused row. Focus moved to another interactive control is respected. Rows use standard WPF controls, so keyboard navigation and baseline screen-reader semantics come from those controls; custom accessibility metadata and comprehensive UI Automation coverage remain future work.

## Verification

Run `dotnet test "tests/UI Framework.Checks"`. The suite discovers the current regression checks; use the generated TRX for current counts and results. Historical counts in dated evidence describe earlier revisions.

Coverage includes primitive edits, current callbacks, duplicate keys, conditional subscriptions, disposal, bounded mounting with 1,000 rows, offscreen insertion/removal/movement, unaffected control retention during a visible move, correct binding after movement, expanded-height scrolling, offscreen observer release, state restoration, fresh state after removal/reinsertion, and keyboard focus retention in a real WPF window. The scroll round-trip test can retain the original native input. A separate real-window test forces WPF container regeneration with Items.Refresh and requires a new native input, balanced unmount/remount counts, retained component identity, restored keyboard focus, and correct subsequent edits. This exercises reconstruction independently of WPF's scrolling cache; automatic offscreen recycling of a focused row remains a separate scenario.

Most tests arrange WPF controls offscreen. The real-window check establishes keyboard focus retention, but the suite does not yet cover IME behavior, accessibility, frame rate, or whole-application performance. Historical 90-check/21-stress-assertion results in performance-results.md predate this change. The three original suites remain grouped in Test Explorer; new checks are individually discoverable.
