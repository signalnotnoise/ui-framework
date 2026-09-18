# Editors and selectors

The core describes inputs and bindings without depending on WPF. The WPF renderer owns native controls, event suppression, selection, and undo behavior.

```csharp
var topic = new State<string>("");
var transcript = new State<string>("");
var apiKey = new State<string>("");
var providerIndex = new State<int>(0);

View Body() => VStack(
    TextEditor(topic).MaxLength(8000).UndoLimit(100).Height(120),
    TextEditor(transcript).IsReadOnly(true).Height(320),
    PasswordField(apiKey).MaxLength(512),
    Picker(new[] { "Provider A", "Provider B" }, providerIndex)
).Spacing(12);
```

Each factory accepts a `Binding<T>` or `State<T>` (string for text/password, int for selection). They share the existing [binding contracts](bindings.md): reads track dependencies, setters can normalize/reject edits, native controls immediately read back the accepted value, and state-driven patches do not write back. Retained controls use the latest binding. State remains confined to the UI thread.

## TextField and TextEditor

`TextField` is single-line. `TextEditor` accepts returns, wraps text, shows an automatic vertical scrollbar, and disables horizontal scrolling. Set a finite height when placing a transcript in a vertical stack so it has a scrolling viewport.

- `.IsReadOnly(true)` prevents edits while preserving selection and copying. It also disables and clears native undo history. External state updates still display normally.
- `.MaxLength(n)` sets WPF's native user-input limit; `0` means unlimited. It does not truncate existing or externally supplied values and is not application validation. Use a binding setter if all incoming values must be validated.
- `.UndoLimit(n)` bounds native undo actions (default 100); `0` disables undo. Negative limits throw. A read-only editor always disables undo, regardless of the configured limit. Changing the limit clears native undo history; ordinary renders leave the limit untouched.

No-op patches retain selection. On an external text replacement the renderer preserves the caret as far as the new length permits; it does not promise to preserve the old selection range or automatically scroll to the end. The transcript's actual string still belongs to the application: cap it there if total output must be bounded.

## PasswordField

Uses a native WPF `PasswordBox`, including password masking and `.MaxLength(n)`. `.IsEnabled(false)` works as on other views. There is no read-only or undo modifier for passwords.

Masking is a presentation feature. The binding and view description contain managed strings; this API is not secure credential storage. The application owns persistence and secret lifetime.

## Picker

`Picker(IReadOnlyList<string> options, Binding<int> selectedIndex)` uses a non-editable native `ComboBox`. A `State<int>` overload is also available. Options are copied when building the description, and equal option lists do not replace the native item source.

Indices are positional, including when labels repeat. `-1` means no selection. Any bound index outside the options range displays no selection without rewriting application state. Replacing or emptying options does not write a selection back. When options are reordered, the application must update the index if it wants to preserve the selected identity. Use observable state for option changes or explicitly refresh the host.

## Themes and verification

`ThemeStyles.Apply` styles text editors using the text-input template and passwords using an equivalent content-host template. Pickers use a scoped template for both closed chrome and dropdown items, so stock Windows gradients cannot override dark theme backgrounds. Surface/Ink colors pair for ordinary items, Accent/OnAccent for selection, Hover/AccentHover for highlighted items, Pressed for the open control, and Focus for keyboard focus borders. Disabled controls use reduced opacity. The native ComboBox and ComboBoxItem still own keyboard selection, type-ahead, popup capture and dismissal. `.IsEnabled`, `.FontSize`, and the usual layout modifiers apply. The picker template is for the non-editable `UI.Picker` contract.

`EditorTests` covers multiline edits, normalization, read-only selection/undo, password rejection and binding replacement, picker options/invalid indices/duplicate labels, disabled state, disposal, and modifier validation. These are offscreen STA tests; visible keyboard, IME, popup, and screen-reader behavior still require interactive verification.

`PickerStyleTests` also creates a hidden native presentation source and verifies light/dark closed and popup chrome, actual item colors, keyboard Down selection, Escape dismissal, binding propagation, and theme scope. It does not replace visible-window accessibility or pointer-interaction review.
