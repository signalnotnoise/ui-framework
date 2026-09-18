# Layout and scoped styling

The core describes layout and style intent without WPF dependencies. The WPF backend measures native controls, allocates space, and supplies interaction templates. Existing HStack and VStack keep their content-sized stack behavior.

## Flexible rows

```csharp
FlexRow(
    Text("Navigation").Width(180),
    TextField(query).Flex(2),
    Button("Search", Search).Flex(1)
).Spacing(12);
```

`FlexRow` reserves explicit child widths and gaps, then distributes remaining finite width by `Flex` weight (default 1). `Flex(0)` sizes a child to its content. Explicit Width takes precedence. Under an unbounded horizontal parent, WPF measures star columns to content; use a finite viewport to get proportional sizing. Fixed content wider than the viewport does not automatically shrink.

`Align(horizontal, vertical)` positions the view's frame within its allocated space. Each axis accepts Stretch, Start, Center, or End; defaults are Stretch. Width/Height constrain the frame independently of alignment.

## Adaptive columns

```csharp
AdaptiveGrid(240, planned, building, shipped).Spacing(16);
```

The renderer chooses as many equal columns as fit the available width, up to the number of children. Rows use the tallest measured child. Below the requested minimum column width, one column shrinks to fit. With infinite available width, all children occupy a single row at the requested column width. This is layout, not virtualization: every child is mounted. Resizing rearranges existing native controls without rebuilding components. Keep the full-list stress baseline for virtualization comparisons.

## Theme boundaries

```csharp
var theme = new ThemeTokens { Accent = "#176B57", ControlRadius = 8 };
ThemeStyles.Apply(host, theme); // UI_Framework.Wpf
```

ThemeTokens is an immutable core record for canvas, surface, text, interaction colors, control radius, and control padding. Sample views consume the surface and typography colors; scoped WPF Button and TextBox styles consume the control tokens. Apply another theme at the same boundary to replace these styles. Application-wide resources are not modified. Local WPF property values can override style values using standard WPF precedence.

`ButtonStyle(Primary | Secondary | Quiet)` chooses button appearance. The native templates provide hover, pressed, keyboard-focus, and disabled feedback. `IsEnabled(false)` disables the entire view subtree. Text fields retain native editing and get hover/focus borders. Explicit Foreground on a native control overrides the theme; removing it restores theme precedence. Toggle and scrollbar styling remains native. Screen-entry transitions are documented under [navigation](navigation.md); there is no general animation or responsive visibility API.

## Ownership and verification

- Core `Layout/` and `Styling/`: platform-independent descriptions and tokens.
- WPF `Layout/AdaptivePanel.cs`: measurement and arrangement.
- WPF `Styling/ThemeStyles.cs`: scoped native resources, templates, and interaction states.
- Sample `LaunchTheme`: application palette; Launchpad composes the responsive screen.

Focused regression checks cover proportional allocation after fixed widths/gaps, adaptive reflow without losing input identity or selection, themed binding and disabled-state updates, explicit foreground precedence/removal, empty-grid sizing, and invalid layout values. All 33 Release checks passed on September 18, 2026. Wide and compact Launchpad previews are generated with `--showcase --capture` and `--showcase --capture --compact`.
