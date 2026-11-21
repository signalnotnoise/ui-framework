# Button tooltips (1.3.0)

FW-040 adds `View.Tooltip(string?)` and `View.TooltipText` for `Button` and
`IconButton`, including buttons with both an icon and label. These additions
are in the 1.3.0 API inventory, **not published 1.2.0**.
See [release status](releases/1.3.0.md) for publication evidence. Verify the
consumer's package metadata before using them.

```csharp
Button("Save", Save).Tooltip("Save document (Ctrl+S)");
IconButton(AppIcons.Search, "Search", Search).Tooltip("Find in document (Ctrl+F)");
```

The tooltip supplements the accessible name. It does not register a shortcut
or change the button's automation name. Applications own localized descriptions,
shortcut labels and reusable icon definitions.

- A nonempty string supplies an explicit hint, overriding a native style hint.
- `Tooltip(null)` restores automatic behavior. Plain buttons return to native
  style/default values; icon-only buttons use their accessible name. Labelled
  icon buttons retain their existing automatic behavior (no generated hint).
- `Tooltip("")` suppresses the hint, including hints supplied by a native style.

Retained updates change the native button's tooltip property without replacing
the button or icon. Recycled rows receive the current description. The core
stores only text; `ButtonTooltip` in the WPF rendering layer applies precedence.
`IconButtonContent` owns icon visuals, not tooltip policy. No extra timer, event
subscription, tooltip cache or timing/color constants are introduced.

Presentation, opening/closing, keyboard-focus behavior, disabled-control behavior
and timing remain WPF's native tooltip service policy. Application styles can
configure `ToolTipService` properties; the renderer does not reset them. A hint
is not guaranteed to appear on disabled buttons or on every focus transition.
Keep important instructions visible and give icon-only buttons meaningful names.
System tooltip presentation is not a new framework-themed popup.

The modifier rejects other view kinds; direct descriptions with tooltip text on
unsupported kinds fail validation before the previous tree is replaced. Native
islands retain their application-owned native tooltips. Rich tooltip content,
container inheritance, custom placement and a cross-platform popup API are
outside this first scope.

Automated checks cover retained identity, accessibility-name independence,
style precedence, suppression/restoration, content transitions, row regeneration
and removal, and preservation of native policy. Property assertions are not
physical keyboard, screen-reader or visible-popup acceptance. See the
[FW-040 validation record](fw040-tooltips-2026-11-21.md) for measured evidence
and remaining acceptance limits.
