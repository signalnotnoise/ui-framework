# Explicit accessibility labels

Use `AccessibilityLabel(string? label)` to give a view's native WPF element a contextual automation name without changing its visible text:

```csharp
Button("Inspect", InspectEvidence).AccessibilityLabel("Inspect Evidence t2.e1");
Button("−", CollapseClaim).AccessibilityLabel("Collapse Claim t1.c1");
```

The modifier stores `View.AccessibleName` in the platform-independent description. WPF maps it to `AutomationProperties.Name` on the native control. Changing the label updates the retained control. Passing `null` clears the override so the native automation peer can derive its normal name from content again. An empty string is passed through to WPF; use null to remove the local value explicitly.

Buttons retain their native focus, keyboard, click, and automation behavior. For a component, put the modifier on the actionable view returned by its body when that control needs the name. A label on a container names the container; it does not label all descendants.
