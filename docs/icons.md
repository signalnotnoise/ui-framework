# Vector icons and icon buttons (1.2.0)

These APIs require version 1.2.0 or later; they are absent from 1.1.0.
Publication status is tracked in [the release record](releases/1.2.0.md).
Core descriptions remain independent of WPF. No SVG document loader or bundled
icon collection is included.

## Usage

Define reusable icons once in an application-owned file such as `Icons/AppIcons.cs`:

```csharp
using UI_Framework;

internal static class AppIcons
{
    private const double OutlineWidth = 2;
    internal static readonly VectorIcon Add =
        new("M 12,4 V 20 M 4,12 H 20", strokeWidth: OutlineWidth);
}
```

Compose them with the existing layout and button APIs:

```csharp
Icon(AppIcons.Add); // Decorative by default.
Icon(AppIcons.Add).AccessibilityLabel("Additional items available");
Button(AppIcons.Add, "Add item", AddItem).ButtonStyle(ButtonStyleKind.Primary);
IconButton(AppIcons.Add, "Add item", AddItem);
```

`AddItem` is an application callback. Use existing application design tokens for
explicit size/color overrides. `Icon(icon, size)` sets a square DIP box; `FontSize`
also controls that box. On icon buttons `FontSize` scales both icon and label.
`Width`/`Height` still size the outer view frame, not the glyph. The scoped `theme.Icons.LabelGap` controls icon-to-label spacing. Display defaults
live in `IconTokens`, while `IconDefaults` owns path viewport coordinates;
application geometry belongs in reusable icon definitions. See [shared tokens](design-tokens.md).

## Contract

- `VectorIcon` is immutable and contains path data, a zero-origin viewport and
  stroke width. Its default viewport is 24 by 24 coordinate units. A zero stroke
  width fills the path; a positive width produces a rounded outline without fill.
  Use separate definitions for variants. Multi-color icons are outside this slice.
- WPF accepts its path mini-language (`M`, `L`, `H`, `V`, `C`, `Q`, `A`, `Z`, etc.),
  not SVG XML, CSS or external assets. Validate/export geometry from your chosen
  icon set and honor its license. WPF validates syntax before patching a tree.
- Viewport dimensions and display size must be finite and positive; stroke width
  must be finite and nonnegative. Geometry is aspect-preserving, centered inside
  the square box and clipped to the declared viewport, including stroke overflow.
- Reuse definitions in static fields or other retained owners. WPF caches frozen
  geometry by definition identity in a weak-key cache; repeated renders do not
  parse paths, and dropped definitions are not held by a global string cache.
- Icon colors inherit the surrounding foreground. Button icons follow native
  button foreground, including primary/disabled/contrast states. Explicit
  `.Foreground(...)` overrides retain existing precedence. Standalone icons need
  the same root/theme foreground setup as standalone text.
- `Button(icon, text, click)` requires a visible nonblank label. `IconButton`
  requires a nonblank accessible name and shows it as a tooltip. Direct record
  updates cannot remove the name from an icon-only button. Standard button
  styling, keyboard activation, focus, disabled state and click cleanup remain
  native. Content changes retain the button; icon/text changes retain its content.
- Standalone icons are decorative unless labelled. Named icons expose the Image
  automation role. Icons inside buttons are decorative and never separate Tab
  stops. No new physical Narrator acceptance is claimed.

## Verification

`IconTests` covers invalid descriptions before mutation, frozen shared geometry,
retained content and callbacks, automation names/roles/invoke/disabled behavior,
theme and contrast inheritance, label wrapping and filled/outlined rendering at
96, 144 and 192 DPI. `ViewValidationTests` includes the new kind. Performance
comparisons retain full-list, virtualized and themed full-list workloads; see
FW-037 in [the backlog](backlog.md) for evidence and limitations.
