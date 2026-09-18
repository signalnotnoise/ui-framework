# Data binding

`State<T>` owns observable data. `Binding<T>` is a live read/write connection to data owned elsewhere. Its internal derived observer subscribes to source state only while a render session observes it, and notifies that session only when the selected value changes. It does not own an independently editable copy.

```csharp
var name = new State<string>("Alex");
var field = TextField(name.Binding());
var enabled = new State<bool>(false);
var toggle = Toggle("Enabled", enabled.Binding());
```

The original `TextField(State<string>)` overload still works. Toggle also accepts `State<bool>` directly. [TextEditor, PasswordField, and Picker](editors.md) accept bindings or state and use the same accepted-value readback contract.

## Project a record field

```csharp
public sealed record Preferences(bool DarkMode, string Language);
public sealed record Settings(string Name, Preferences Preferences);

var settings = new State<Settings>(new("My project", new(false, "English")));
var name = settings.Binding(s => s.Name, (s, value) => s with { Name = value });
var preferences = settings.Binding(s => s.Preferences, (s, value) => s with { Preferences = value });
var darkMode = preferences.Select(p => p.DarkMode, (p, value) => p with { DarkMode = value });

View Body() => VStack(TextField(name), Toggle("Dark mode", darkMode));
```

Each projection setter receives the latest parent value at write time. Chained projections reconstruct each containing record without overwriting newer changes to unrelated fields. The setter should return a replacement value; mutating a reference object in place can suppress the source's equality-based notification.

A projection compares its result using `EqualityComparer<T>.Default`. Editing a different field in the source record re-evaluates the selector but does not rebuild readers of an unchanged result. Reading the entire `State<T>.Value` still observes that whole value. State-backed projection chains are flattened onto their root source to avoid repeated intermediate observation work.

State-backed selectors/projectors should depend only on the supplied record/value and stable non-observable inputs. For a getter that combines multiple observable sources, use a custom Binding constructor or `Computed<T>` so those sources are discovered dynamically. Getters must be pure and safe to evaluate when their source changes. Store frequently used bindings/selectors in fields to reuse their observation objects across builds.

## Child editors

```csharp
public sealed class NameEditor : Component
{
    public Binding<string> Name { get; set; } = null!;
    public override View Body() => TextField(Name);
}

View Body() => Component<NameEditor>(editor => editor.Name = name);
```

The parent keeps ownership. A child accepts only the connection it needs. Multiple controls can share a binding, and a retained control always uses the binding in its latest view description. Changing the binding source replaces its dependency on the next build.

## Custom adapters

```csharp
var uppercase = new Binding<string>(
    get: () => name.Value,
    set: value => name.Value = value.Trim().ToUpperInvariant()
);
```

TextField and Toggle read back the accepted value after user edits. A setter may normalize or reject an edit even when the underlying state does not change. Programmatic control updates suppress feedback events. This is synchronous normalization, not a validation/error-message system.

A getter that reads plain, non-observable data has no automatic notifications. Either have it read `State<T>`/`StateList<T>` or explicitly refresh its owning host after external changes (and update any Memo inputs). Binding observation, derived values, and state are confined to their creating thread; a custom setter is responsible for its own external-data access rules. Bindings do not marshal threads, synchronize writes, handle async values, or add transactions.
