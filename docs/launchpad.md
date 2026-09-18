# Launchpad showcase

Launchpad turns the existing framework primitives into a small release-planning application. Run `dotnet run --project samples/Counter -- --showcase`.

## Try it

1. Start on **Overview**, then choose **Open release board**. The shell includes Overview, Board, Back, and Reduce motion.
2. Search for Maya and turn off **Show shipped**. Open **A warmer first impression** to visit its Details screen.
3. Edit its title or owner and choose a stage. Click **Back**: the board reflects your edits and retains filters and the local Show shipped preference.
4. Enter a title above the board and click **Add an idea**. It appears in Planned. Adding clears search and priority filters; Add is disabled for blank titles.
5. Choose **Overview** to see updated totals. This pops Board; reopening it resets its local Show shipped preference. Reduce motion disables entry animations.

All data lasts for the current session. There is no server, persistence, collaboration, or drag-and-drop. Board columns and Details panels reflow as the window narrows; the minimum width is 760. Navigation retains logical state but recreates controls, so selection and scroll positions are not preserved across screens.

## Implementation

`LaunchModel` owns items, filters, history, and workflow actions. LaunchRoute and LaunchScreen define application routes. Launchpad is the shell; LaunchOverview, LaunchBoard, and LaunchDetails define screens. LaunchItem owns editable fields and LaunchTheme supplies design tokens. LaunchpadWindow owns native window lifetime and preview rendering. Reusable history belongs to the core; mounting, snapshots, and animations belong to WPF.

Run `dotnet run --project samples/Counter -- --showcase --capture` for `artifacts/launchpad/overview.png`. Add `--board` or `--details` to start on that screen; add `--compact` for an 800-pixel preview with a `-compact` filename suffix. Use `--navigation-check` for the real-window stress sequence. The original stress and comparison modes remain available. See [navigation](navigation.md) and [layout and styling](layout-styling.md).
