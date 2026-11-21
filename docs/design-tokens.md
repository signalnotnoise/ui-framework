# Shared design tokens (1.2.0)

Version 1.2.0 extends `ThemeTokens` with immutable spacing, typography,
corner-radius and icon groups. Version 1.1.0 does not include these APIs.
Publication status is tracked in [the release record](releases/1.2.0.md).
Existing colors, `ControlPadding` and `ControlRadius` remain compatible.

## One theme per scope

Create the theme at the application or workspace boundary. Pass the same instance
to components and `ThemeStyles.Apply(host, theme)`; avoid a new theme per render.
Each component selects semantic values from that instance:

```csharp
VStack(
    Text("Release details").FontSize(theme.Typography.Heading),
    Text("Choose the next milestone.").FontSize(theme.Typography.Body),
    Icon(AppIcons.Add, theme.Icons.Medium),
    Button(AppIcons.Add, "Add milestone", AddMilestone)
        .FontSize(theme.Typography.Body)
).Spacing(theme.Spacing.Medium)
 .Padding(theme.Spacing.ExtraLarge)
 .CornerRadius(theme.Radii.Large)
 .Foreground(theme.Ink).Background(theme.Surface);
```

`theme`, `AppIcons` and `AddMilestone` are application-owned. See the
[component recipe](../skills/ui-framework/references/composition.md) for passing
one theme through component props. Existing numeric modifiers remain supported.

## Default scales

All values are DIPs, except path geometry which remains in its own coordinates.

- `Spacing`: ExtraSmall 4, Small 8, Medium 12, Large 16, ExtraLarge 24, Section 32.
- `Typography`: Caption 12, Body 16, Subheading 20, Heading 24, Title 32.
- `Radii`: Small 4, Medium 8, Large 12.
- `Icons`: Small 16, Medium 24, Large 32, LabelGap 6.

Defaults live in `SpacingTokens`, `TypographyTokens`, `RadiusTokens` and
`IconTokens`, with shared immutable `Default` instances. There is no mutable global
theme. New `ThemeTokens` instances reuse those groups until explicitly overridden.
The default text and icon sizes reference these definitions. Icon path viewport
coordinates are not UI spacing and remain in `VectorIcon`/`IconDefaults`.

Use scale values for repeated design decisions. Keep product-specific pane widths,
breakpoints and other semantic dimensions in the application's design settings.
Do not replace unrelated equal numbers with the same token solely because they
currently share a value. Ordinary zero/one sentinels and vector coordinates do
not need design-token wrappers.

## Overrides and updates

```csharp
var next = theme with
{
    Typography = theme.Typography with { Body = 18 },
    Spacing = theme.Spacing with { Medium = 16 },
    Icons = theme.Icons with { LabelGap = 8 }
};

theme = next; // The host's body reads this owner-held theme.
ThemeStyles.Apply(host, theme);
host.Refresh();
```

Token selection in view composition copies numeric values, just like existing
modifiers. Changing `Typography.Body` does not automatically resize views that
never selected it. Recompose after replacing the theme; retained controls update
without being replaced. Explicit sizes continue to win. No inherited font/layout
role engine or automatic binding to arbitrary token properties is introduced.

`Icons.LabelGap` is consumed directly by scoped native icon-button content styles.
Reapplying a theme updates the gap without replacing buttons or requiring a body
refresh. Nested theme boundaries remain independent. Icon-only buttons have zero
label gap. The unthemed gap uses `IconTokens.Default.LabelGap`.

`ControlPadding` and `ControlRadius` are existing semantic control values.
Their defaults stay 10 and 8. Changing `Radii.Medium` does not silently change
`ControlRadius`; explicitly assign it when that is the intended relationship:
`theme with { ControlRadius = theme.Radii.Medium }`.

`ThemeStyles.Apply` validates every numeric scale before changing resources.
Spacing, radii and label gap must be finite and nonnegative; font/icon sizes must
be finite and positive. Null groups are rejected. Values need not be ordered or
unique. Invalid values passed directly to view modifiers still follow their
existing validation rules.

## Ownership and validation

Core owns the values; WPF owns native style application and scoped icon-label gap
updates. Applications own theme lifetime, custom scales and component props.
There are no WPF types in core, per-view theme objects or global theme subscriptions.
`DesignTokenTests` covers immutable sharing, all numeric members' validation,
zero-value rules, retained/nested updates and explicit token-based composition.
See FW-038 in [the backlog](backlog.md) for measured performance and acceptance.
