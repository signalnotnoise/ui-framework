# Vector icons and icon buttons (1.2.0)

These APIs require version 1.2.0 or later; they are absent from 1.1.0.
Publication status is tracked in [the release record](releases/1.2.0.md).
Core descriptions remain independent of WPF. No SVG document loader or bundled
icon collection is included.

Version 1.3.0 / FW-040 adds [independent tooltip descriptions](tooltips.md).
That modifier is not part of published 1.2.0; the icon-only accessible-name
fallback remains available in 1.2.0.

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

## Compact and flat icon buttons

`IconButton(...).Width(32).Height(32)` sizes the **outer view frame**, not
the native Button or the icon. `.Padding(0)` also affects that frame. The
renderer creates Buttons with local native padding and left alignment; those
values take precedence over ordinary style setters. `.Align(...)` changes the
frame's alignment, not the native Button's alignment.

Removing native padding alone does not make the button fill the frame. In the
FW-041 reproduction, a 32-DIP frame contains an 18-DIP content-sized native button
with the framework border, or a 16-DIP button with a reconstructed borderless
template. Explicit native width/height establishes the intended hit target.
This explains the reported sizing interaction without claiming to have tested
GuidedGrade's actual custom template or changing `.Width()` semantics.

The [compiled toolbar recipe](../skills/ui-framework/references/behavior-examples.md#skillicontoolbarexample)
uses existing 1.2.0 APIs:

```csharp
var example = new SkillIconToolbarExample(theme, Search);
var toolbar = new ViewHost(example.Body);
example.ApplyTheme(toolbar, theme, SystemParameters.HighContrast);
// Attach toolbar to the window only after configuring its dedicated scope.
// Its application owner disposes toolbar when removed or closed.
```

The example owns one named `TargetSize` for its desktop hit target. Icon display
size comes from `theme.Icons.Small`; gaps/insets come from `theme.Spacing.Small`.
Those roles remain independent even when two token values happen to be equal.
Vector coordinates stay with the shared icon definition, not in spacing tokens.
The 32-DIP target is an example choice, not a general accessibility minimum.

The dedicated toolbar derives its native Button style from the framework style,
sets native width/height and zero padding, and explicitly clears renderer-local
padding on `Loaded` so its style owns that property. This handoff is intentional
and repeats on reload; do not apply it to a mixed scope containing native buttons
whose local padding/bindings belong to another owner. It does not require walking
the visual tree or changing the renderer.

Use `ButtonStyle(ButtonStyleKind.Quiet)` for a transparent normal border and
background. Keep the framework template and its border thickness: hover, pressed,
focus and disabled feedback remain native. Setting a local transparent brush or
removing the border can hide that feedback. A replacement consumer template owns
its own equivalent behavior; the recipe does not repair arbitrary templates.

The application theme owner must reapply `ApplyTheme` after theme/contrast changes,
passing the current `SystemParameters.HighContrast` value. The recipe rebases on
the newly installed framework style and uses system surface/text colors in
contrast mode. Quiet buttons are transparent, so a fixed dark application canvas
can otherwise conflict with a system-colored foreground. The example does not
subscribe globally to system events; that lifetime remains application-owned.

## Verification

`IconTests` covers invalid descriptions before mutation, frozen shared geometry,
retained content and callbacks, automation names/roles/invoke/disabled behavior,
theme and contrast inheritance, label wrapping and filled/outlined rendering at
96, 144 and 192 DPI. `ViewValidationTests` includes the new kind. Performance
comparisons retain full-list, virtualized and themed full-list workloads; see
FW-037 in [the backlog](backlog.md) for evidence and limitations.

FW-041 adds two frame/native sizing reproductions and eight scoped-toolbar cases
at 320/800 DIP and simulated 100%/200% DPI. They check containment, target size,
retained identity, native invoke/cleanup and template feedback, with saved renders.
See [evidence and limits](fw041-icon-button-sizing-2026-11-21.md). This is a
documentation/example clarification, not a new runtime or tooltip API.
