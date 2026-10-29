# Explicit accessibility labels

Use `AccessibilityLabel(string? label)` to give a view's native WPF element a contextual automation name without changing its visible text:

```csharp
Button("Inspect", InspectEvidence).AccessibilityLabel("Inspect Evidence t2.e1");
Button("−", CollapseClaim).AccessibilityLabel("Collapse Claim t1.c1");
```

The modifier stores `View.AccessibleName` in the platform-independent description. WPF maps it to `AutomationProperties.Name` on the native control. Changing the label updates the retained control. Passing `null` clears the override so the native automation peer can derive its normal name from content again. An empty string is passed through to WPF; use null to remove the local value explicitly.

Buttons retain their native focus, keyboard, click, and automation behavior. For a component, put the modifier on the actionable view returned by its body when that control needs the name. A label on a container names the container; it does not label all descendants.

## Virtualized rows (unreleased)

The list exposes WPF's `ItemContainer` and `Scroll` patterns. Logical row peers
expose `VirtualizedItem` while unrealized, allowing an automation client to find
and realize a row before navigating its actionable children. Discovery uses the
row description's `AccessibilityLabel`, falling back to its direct `Content`.
A component or compound row has no inferred descendant name: label the row for
discovery and label its editors/buttons separately for their individual actions.
Keys preserve identity; they are not automatically spoken names or automation IDs.

```csharp
VirtualList(items.Select(item =>
    HStack(TextField(item.Title).AccessibilityLabel($"Title for {item.Id}"),
           Button("Open", () => Open(item)).AccessibilityLabel($"Open {item.Id}"))
    .Id(item.Id).AccessibilityLabel($"Record {item.Id}")), 400)
    .AccessibilityLabel("Records");
```

WPF owns item discovery, weak peer caching, scrolling and realization. Finding a
row by name scans logical items; it does not make description processing or
search constant time. Realization preserves the bounded native viewport. These
peers add no selection model to `VirtualList`; use the native child control's
patterns. Theme replacement retains compatible native controls and their focus.
`ViewHost` and virtual-row lifetime wrappers are not keyboard destinations by
default. Tab and Shift+Tab reach their actionable descendants, including rows
beyond the initial viewport. Applications can explicitly override native host
focus properties where they deliberately implement a host-level interaction.

## FW-006 acceptance

See the [dated acceptance record](accessibility-2026-10-26.md). Automated peer
checks establish provider behavior, not what a screen reader actually speaks.
The user reported passing Narrator and physical scaling checks on October 28.
Scoped native styles now follow Windows contrast settings using system window,
text, highlight, highlight-text and gray-text colors. System changes update
shared brushes on their owning dispatcher without replacing control templates.
Leaving contrast mode restores the supplied tokens. Disabled controls use opaque
muted text and surface colors rather than fading their entire visual tree.
Explicit local styles/colors and application-owned backgrounds remain the
application’s responsibility, including native islands. The user confirmed a passing physical Windows contrast-switching check on
October 29, closing FW-006 alongside earlier Narrator/scaling passes. See the
[manual acceptance record](manual-acceptance-2026-10-29.md); injected palette tests
remain separate evidence. These additions are not in published alpha.5.


TextBox now uses shared frozen normal colors and conditional system-color triggers,
while other controls retain the bound palette path. See the [TextBox prototype
results](textbox-contrast-2026-10-28.md) for correctness and remaining allocation
gates at that stage; subsequent validation and manual acceptance close FW-006.


The subsequent [resource-batching change](theme-resource-batching-2026-10-28.md)
clears both remaining allocation gates in the latest seven-pair run (32/32).
The subsequent discovery-test corrections and user-confirmed physical acceptance
complete FW-006; see the dated acceptance record above.
