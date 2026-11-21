# SignalNotNoise.UI

Declarative C# components, reactive state and native WPF rendering. Requires .NET 10; v1 acceptance covers Windows 11 x64. Supported source, binary and documented behavioral contracts are preserved within major version 1. See the repository API contract for ownership, threading and virtualization rules.

- `SignalNotNoise.UI`: platform-independent view descriptions, components, state, and bindings.
- `SignalNotNoise.UI.Wpf`: WPF renderer and `ViewHost`; automatically references the core package.

## Get started

In a .NET 10 WPF application, install the package:

```powershell
dotnet add package SignalNotNoise.UI.Wpf --version 1.2.0
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

For applications with substantial WPF text editing, install one cleanup owner
before constructing controls and close it after disposing windows and native
hosts:

```csharp
var app = new Application();
using var cleanup = new UI_Framework.Wpf.WpfComCleanupPolicy(
    app.Dispatcher, error => Trace.WriteLine(error));
app.Run(window);
```

The policy changes CLR COM-wrapper cleanup for the lifetime of that STA and
cannot restore eager cleanup. The application owns failure reporting and shutdown
ordering.

## Supported scope and limits

V1 acceptance covers Windows 11 x64 / .NET 10 and the documented source, binary
and behavioral contracts. Historical physical focus/IME and contrast acceptance
has recorded environment limits; renewed physical Narrator verification after
peer-cache cleanup remains a post-v1 follow-up. Rendering does not roll back
arbitrary native mutations. Virtualization bounds native realization, not all
description processing or visited-row logical state. Native provider recovery
and application shutdown remain application-owned. Non-Windows renderers are
not included. See the repository API contract and release evidence for scope.

See the [source and examples](https://github.com/signalnotnoise/ui-framework), [documentation](https://github.com/signalnotnoise/ui-framework/tree/main/docs), and [issue tracker](https://github.com/signalnotnoise/ui-framework/issues). Licensed under MIT.
