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

Frame sizing does not assign native control dimensions. In particular, declarative
Buttons are left-aligned and have renderer-local native padding. A wider frame
does not guarantee a wider native button, and outer `.Padding(0)` does not clear
native padding. Native local values also outrank ordinary style setters. For
compact icon actions, distinguish frame size, native hit target, native content
padding and icon viewport; see the [scoped toolbar recipe](icons.md#compact-and-flat-icon-buttons).
It preserves the existing sizing contract and native feedback rather than making
all Buttons stretch or changing their defaults.

## Adaptive columns

For vertical fill, fixed edge docking and bound resizable panes, see [workspace layout](workspace-layout.md). These APIs are included in published alpha.7; older application package pins must be upgraded explicitly.

```csharp
AdaptiveGrid(240, planned, building, shipped).Spacing(16);
```

The renderer chooses as many equal columns as fit the available width, up to the number of children. Rows use the tallest measured child. Below the requested minimum column width, one column shrinks to fit. With infinite available width, all children occupy a single row at the requested column width. This is layout, not virtualization: every child is mounted. Resizing rearranges existing native controls without rebuilding components. Keep the full-list stress baseline for virtualization comparisons.

## Shared design values

Version 1.2.0 adds `theme.Spacing`, `theme.Typography`, `theme.Radii` and
`theme.Icons`. Select them in existing view modifiers and pass the same theme
through component props. See [shared design tokens](design-tokens.md) for defaults,
validation and theme replacement behavior; these groups are not in NuGet 1.1.0.

## Theme boundaries

```csharp
var theme = new ThemeTokens { Accent = "#176B57", ControlRadius = 8 };
ThemeStyles.Apply(host, theme); // UI_Framework.Wpf
```

`ThemeStyles.Apply` installs control styles; it does not set the host's Foreground/Background or a global TextBlock style. Standalone dark surfaces must set root colors explicitly, for example `.Foreground(theme.Ink).Background(theme.Canvas)` on the declarative root, or WPF `host.Foreground`/`host.Background` in the application's theme helper. Otherwise text inherits colors from its surrounding WPF surface.

`UI.Text` wraps by default. Wrapping requires finite available width; an unconstrained `HStack` can measure children with infinite horizontal space. Use a constrained width or a `FlexRow` inside a finite-width surface for wrapping descriptions.

ThemeTokens is an immutable core record for canvas, surface, text, interaction colors, control radius, and control padding. Sample views consume the surface and typography colors; scoped WPF Button, TextBox, PasswordBox, ComboBox and CheckBox styles consume the control tokens. Apply another theme at the same boundary to replace these styles. Application-wide resources are not modified. Local WPF property values can override style values using standard WPF precedence.

`ButtonStyle(Primary | Secondary | Quiet)` chooses button appearance. The native templates provide hover, pressed, keyboard-focus, and disabled feedback. `IsEnabled(false)` disables the entire view subtree. Text fields retain native editing and get hover/focus borders. Explicit Foreground on a native control overrides the theme; removing it restores theme precedence. Scrollbar styling remains native. Screen-entry transitions are documented under [navigation](navigation.md); opacity/translation targets are covered by [animation](animation.md); there is no responsive visibility API.

The scoped Button template presents native `Content`, including panels containing labels and interactive controls, through a `ContentPresenter`. It forwards `ContentTemplate`, `ContentTemplateSelector`, and `ContentStringFormat` using WPF's normal template selection. Ordinary string labels retain wrapping and inherited theme foreground. Application-owned visuals and explicit or implicit data templates retain their own wrapping policy. This does not add a declarative rich-button API; native compositions remain application-owned. Menus, context menus and scrollbars still need application styles.

Since `0.1.0-alpha.5`: button chrome now binds to the native control's
`Padding`, including local overrides and live bindings. Without an override,
`theme.ControlPadding` still supplies the native style's padding. Clearing a
local padding value restores that theme default. Declarative buttons keep
their existing native padding of `(12, 6, 12, 6)`; the chrome now honors it
instead of silently drawing the theme's different padding. Their vertical
chrome may therefore be tighter than in alpha.4; colors, content presentation
and interaction states are unchanged.

Automatic native button sizing retains the 36-DIP theme minimum. Setting the
native button's `Height` disables that default minimum, so an 18-DIP close
button with `Padding=0` and a suitably sized glyph can fit without a custom
style. An explicit native `MinHeight` still takes precedence. Clearing Height
restores automatic sizing and its minimum. This applies to native button
properties; view `.Height(...)` and `.Padding(...)` continue to size/pad the
outer framework frame. Callers must still allow enough room for their chosen
font, border and native padding. See [compact button validation](compact-button-validation.md).

`Toggle` remains a native CheckBox with the same boolean binding API. Its scoped template uses Ink for labels, Accent/OnAccent for checked indicators, Hover/Pressed variants for interaction, and a Focus-colored outline while keyboard focus is inside. Disabled labels and checkmarks use Muted at full opacity against Surface; they do not fall back to system black/gray text or fade the entire control. An explicit `.Foreground(...)` on the Toggle takes precedence, including when disabled. The template covers the framework's string-label, two-state Toggle contract. This behavior is included in published alpha.7; the old alpha.1 package predates the fix.

## Ownership and verification

### Validation and error appearance (v1 contract)

Validation is application-owned composition. The application owns rules, when to
validate, error state, save/publish eligibility and the visible messages. The
framework does not promise a validation engine, error token, error adorner or
automatic screen-reader announcement in v1. Native WPF validation features may
be used inside application-owned native content; they are not a declarative
framework validation API.

Keep a visible label and place a short, actionable message near the affected
field, such as "Error: Enter a release name before saving." Use ordinary readable
theme text; color can supplement the wording but must not carry the error alone.
Update the input's `AccessibilityLabel` with relevant error context and clear or
replace both visible and accessible text after correction. This provides a field
description, not a live-region announcement guarantee. Explain disabled actions.
Applications own custom colors and Windows contrast behavior on their own surfaces;
using an accent or a red border is not a substitute for that responsibility.

The [appearance acceptance fixture](../tools/SkillPreview/AppearanceScreen.cs)
demonstrates this contract without adding an API. Its invalid-to-valid-to-invalid
sequence checks visible messages and accessible context; the [acceptance record](fw017-appearance-acceptance-2026-11-15.md)
includes light/dark captures and retained-control checks.

- Core `Layout/` and `Styling/`: platform-independent descriptions and tokens.
- WPF `Layout/AdaptivePanel.cs`: measurement and arrangement.
- WPF `Styling/ThemeStyles.cs`: scoped native resources, templates, and interaction states.
- WPF `Styling/ButtonContentPresenter.cs`: native content presentation with wrapping for ordinary generated string labels; application content and templates remain untouched.
- WPF `Styling/ToggleStyles.cs`: checkbox label, indicator, focus and disabled visuals; native CheckBox still owns input and automation.
- Sample `LaunchTheme`: application palette; Launchpad composes the responsive screen.

Focused regression checks cover proportional allocation after fixed widths/gaps, adaptive reflow without losing input identity or selection, themed binding and disabled-state updates, explicit foreground precedence/removal, empty-grid sizing, and invalid layout values. All 33 Release checks passed on September 18, 2026. Wide and compact Launchpad previews are generated with `--showcase --capture` and `--showcase --capture --compact`.

`ToggleStyleTests` adds light/dark rendered-label and indicator checks, disabled contrast tokens, native UI Automation toggle/binding behavior, foreground precedence and removal, theme replacement, and focus-outline activation. Focus appearance is tested by driving WPF's focus-state property on a hidden presentation source; it is not a visible-window keyboard-focus or screen-reader campaign.
