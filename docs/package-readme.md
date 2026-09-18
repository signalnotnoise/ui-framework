# SignalNotNoise.UI

Experimental SwiftUI-inspired UI components in C#. Requires .NET 10; the WPF renderer requires Windows. APIs may change without compatibility guarantees.

- `SignalNotNoise.UI`: platform-independent view descriptions, components, state, and bindings.
- `SignalNotNoise.UI.Wpf`: WPF renderer and `ViewHost`; automatically references the core package.

## Get started

In a .NET 10 WPF application, install the preview:

```powershell
dotnet add package SignalNotNoise.UI.Wpf --version 0.1.0-alpha.1
```

In your WPF window constructor, after `InitializeComponent()`:

```csharp
var count = new UI_Framework.State<int>(0);
var host = new UI_Framework.Wpf.ViewHost(() => UI_Framework.UI.VStack(
    UI_Framework.UI.Text($"Count: {count.Value}").FontSize(24),
    UI_Framework.UI.Button("Increment", () => count.Value++)
).Spacing(12).Padding(20));
Content = host;
Closed += (_, _) => host.Dispose();
```

Create and access state on the UI thread. State reads during view builds become subscriptions; updates are batched. Dispose the host when its window closes. Use stable `.Id(...)` keys for dynamic sibling components.

## Preview limitations

This is an evaluation release, not a production SwiftUI replacement. Visible-window focus/IME and accessibility behavior still need validation. Rendering is not transactional, some large list shuffles have quadratic cost, and non-Windows renderers are not available.

See the [source and examples](https://github.com/signalnotnoise/ui-framework), [documentation](https://github.com/signalnotnoise/ui-framework/tree/main/docs), and [issue tracker](https://github.com/signalnotnoise/ui-framework/issues). Licensed under MIT.
