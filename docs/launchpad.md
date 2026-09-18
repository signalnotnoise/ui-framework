# Launchpad showcase

Launchpad turns the existing framework primitives into a small release-planning application. Run `dotnet run --project samples/Counter -- --showcase`.

## Try it

1. Select **A warmer first impression** in Building. Edit its title and owner in the details panel; the card updates as you type.
2. Click **Ship it**. The card moves to Shipped and the release totals update. The selected item's editor stays available; use its stage buttons to move the card back.
3. Search for an owner such as Maya, or enable **Priority only**. A column with no matches explains how to get work back into view.
4. Enter an idea above the board and click **Add an idea**. It appears in Planned and opens in the editor. Adding clears the filters so the new card is visible. Blank titles are rejected with a message in the footer.

All data is in memory for the current session. The demo has no server, persistence, collaboration, or drag-and-drop. Columns scroll independently and reflow as the window narrows; the workspace scrolls vertically while the editor keeps a fixed width. The minimum window width is 760. Native WPF buttons and text fields retain keyboard interaction and use scoped theme styles for hover, pressed, focus, and disabled states. The Add button becomes available when the draft has a nonblank title.

## Implementation

`LaunchModel` owns the items, filters, selected item, and workflow actions. `LaunchItem` owns editable observable fields. `Launchpad` composes the board and editor without WPF dependencies. `LaunchTheme` supplies reusable application tokens; `LaunchpadWindow` applies them at the WPF boundary and handles the window and preview rendering. Application behavior stays in the sample; reusable layout and styling live in the framework layers.

Run `dotnet run --project samples/Counter -- --showcase --capture` to render an offscreen preview at `artifacts/launchpad/board.png`. Add `--compact` for an 800-pixel-wide preview at `artifacts/launchpad/compact.png`. The original stress and comparison modes remain available. See [layout and styling](layout-styling.md) for the framework APIs this screen demonstrates.
