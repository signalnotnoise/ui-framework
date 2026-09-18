# Virtualized lists and regression tests

`UI.VirtualList(rows, height)` requires a finite positive viewport height and unique, nonempty row keys. In the stress lab, enable **Virtualized list** to compare against the default full-list mode.

The WPF backend uses a VirtualizingStackPanel with pixel scrolling and a half-page cache on each side. Variable heights are measured by WPF. View descriptions and keyed row metadata exist for the whole list; native row controls and subscriptions exist only for realized rows. Visited rows retain logical component instances in snapshots until their keys leave the supplied list. Filtering a key out counts as removal.

Collection updates use individual insert, remove and move notifications. They no longer reset ItemsSource or dispose every visible host. WPF may recycle the moved row itself; unaffected rows retain controls and text selection. A large shuffle still performs multiple moves and may have quadratic collection-edit cost. This change does not claim constant-time reordering or a measured frame-rate improvement.

On recycling, subscriptions detach and OnUnmounted runs. On return, matching component instances and nested state restore, new native controls are created, and OnMounted runs again. Hooks must support repeated mount/unmount cycles. Resources disposed by OnUnmounted must be recreated by OnMounted. Removing a key or disposing the list releases its saved state. Component fields should not retain native controls.

Focused controls retain focus intent across row recycling and keyed visible-row moves. When a focused row is recreated, focus returns to its first focusable descendant after the new native subtree is attached. Text selection and IME composition are still subject to WPF's control recreation rules and are not guaranteed across an offscreen unmount. Rows use standard WPF controls, so keyboard navigation and baseline screen-reader semantics come from those controls; custom accessibility metadata and comprehensive UI Automation coverage remain future work.

## Verification

Run `dotnet test "tests/UI Framework.Checks"`. Test Explorer now discovers 19 tests: the three existing regression suites plus 16 individual primitive, state/session and virtualization tests. The September 17, 2026 fresh Debug build and all 19 tests passed.

Coverage includes primitive edits, current callbacks, duplicate keys, conditional subscriptions, disposal, bounded mounting with 1,000 rows, offscreen insertion/removal/movement, unaffected control retention during a visible move, correct binding after movement, expanded-height scrolling, offscreen observer release, state restoration, and fresh state after removal/reinsertion. Focus restoration is implemented but still needs a dedicated real-focus regression check.

These tests arrange WPF controls offscreen. They do not establish real keyboard focus, IME behavior, accessibility, frame rate, or whole-application performance. Historical 90-check/21-stress-assertion results in performance-results.md predate this change. The three original suites remain grouped in Test Explorer; new checks are individually discoverable.
