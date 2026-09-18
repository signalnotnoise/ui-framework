# ui-framework

[Source repository](https://github.com/signalnotnoise/ui-framework)

**Experimental · 0.1.0-alpha.1 release candidate · Windows renderer · .NET 10**

APIs may change without compatibility guarantees. This release is intended for evaluation and contributions under the [MIT license](LICENSE). See the [release checklist](docs/releasing.md), [changelog](CHANGELOG.md), and [contributing guide](CONTRIBUTING.md). CI builds and validates preview NuGet packages; publishing is a separate step.

A small SwiftUI-inspired C# UI framework. The core is a .NET class library; the first backend renders WPF controls on Windows. The runtime requires no XAML or third-party packages. Tests use MSTest.

See the [documentation index](docs/README.md) for API guides, architectural decisions, benchmarks, and the next milestone.

## NuGet preview

The package IDs are `SignalNotNoise.UI` (core) and `SignalNotNoise.UI.Wpf` (Windows renderer). The WPF package brings in the core automatically. Version `0.1.0-alpha.1` is prepared for publication; availability on NuGet.org is not implied by this README.

Build and verify local packages with `./tools/Test-Packages.ps1`. Outputs are under `artifacts/packages`. To try the local preview in a .NET 10 WPF project:

```powershell
dotnet add package SignalNotNoise.UI.Wpf --version 0.1.0-alpha.1 --source "C:\path\to\ui-framework\artifacts\packages"
```

See the [package getting-started guide](docs/package-readme.md) and [publishing instructions](docs/releasing.md).

## Run from source

Requires Windows and the .NET 10 SDK. From this directory:

```powershell
dotnet build "UI Framework.slnx"
dotnet run --project samples/Counter
dotnet test "tests/UI Framework.Checks"
```

## Try the product showcase

Launchpad is a release-planning application with Overview, Board, and Details screens, retained state on Back, and reduced-motion-aware transitions. Move cards between Planned, Building, and Shipped, edit details, filter by title/owner/area, and add ideas while release metrics update.

```powershell
dotnet run --project samples/Counter -- --showcase
```

The demo uses session-only sample data. The existing stress lab remains the default. See [Launchpad](docs/launchpad.md) for a short walkthrough.

## Write a view

```csharp
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

var count = new State<int>(0);
var host = new ViewHost(() => VStack(
    Text($"Count: {count.Value}").FontSize(24),
    Button("Increment", () => count.Value++)
).Spacing(12).Padding(20));
```

Place the host in a WPF window and dispose it when the window closes. Create and access observable state on the UI thread. Reads during a build become subscriptions; conditional reads are tracked again each build. Multiple changes are batched through the dispatcher.

## Reusable components

```csharp
public sealed class CounterView : Component
{
    private readonly State<int> count = new(0);

    public override View Body() => VStack(
        Text($"Count: {count.Value}"),
        Button("Increment", () => count.Value++)
    ).Spacing(12);

    public override void OnMounted() { /* Subscribe to external resources here. */ }
    public override void OnUnmounted() { /* Release those subscriptions here. */ }
}

// Use a descriptor so the renderer owns and retains the component instance.
var host = new ViewHost(() => Component<CounterView>().Id("counter"));
```

Keep local state in component fields. The renderer creates each component once for its type and identity. Local state changes rebuild that component's body without rebuilding its parent or unrelated siblings. Parent renders refresh descendants by default. Add `.Memo(immutableInputs)` to skip parent-driven rebuilds when those inputs compare equal; local observed-state updates still render. Include every changing prop and callback input in the comparison.

Pass props using `Component<MyView>(view => view.Title = title.Value)`. Configure runs before each body build on the retained instance, and its state reads are observed by that component. Use ordinary properties for props and reserve state fields for local state. Build/configure functions should describe views without changing observable state.

## Observable lists

```csharp
var rows = new StateList<string>(["a", "b"]);
var host = new ViewHost(() => VStack(
    rows.Select(id => Component<CounterView>().Id(id)).ToArray()
));
rows.Add("c");       // Automatically schedules an update.
rows.Move(0, 2);      // Retains keyed component state.
rows.Remove("b");    // Unmounts the removed component.
```

`StateList<T>` observes enumeration, indexing, and Count. Add, Remove, RemoveAt, Clear, Move, index replacement, and atomic ReplaceAll notify readers. Item property mutations are not deep-observed: use `State<T>` inside an item or replace the item. State rejects access from other threads before a mutation occurs; marshal background results through WPF's dispatcher.

## Data binding and stress app

`Binding<T>` lets a child read and edit parent-owned data. `state.Binding()` connects directly; `state.Binding(get, set)` projects into a record, and `.Select(get, set)` composes deeper projections using the latest parent value. TextField and Toggle accept bindings, synchronize shared editors, and read back normalized/rejected edits without event feedback. See [binding contracts and examples](docs/bindings.md).

Bindings now notify their readers only when the projected value changes. `State.Select(get)` creates a read-only selector; `new Computed<T>(() => ...)` observes a derived value across multiple sources and dynamic branches. Derived subscriptions attach only while observed and detach with the final reader. See [rendering performance contracts](docs/performance.md).

The [first measured optimization pass](docs/performance-results.md) reduced body builds and allocations; the matched 50-operation comparison improved elapsed time by about 10%. Large native lists remain a performance limit.

The Counter app is now a [binding stress lab](docs/stress-lab.md): a project board with up to 1,000 rows, full-list and [virtualized modes](docs/virtualization.md), a shared inspector, nested checklists, filters, project settings, lifecycle/heap counters, recursive components, a 10,000-write burst, and a stoppable mixed-operation campaign. Run it normally for manual testing, or use the documented `--stress` mode to generate measurements and an offscreen dashboard capture.

Additional primitives include `Toggle`, `Scroll`, Width/Height, Background/Foreground, and CornerRadius. `FlexRow`, `AdaptiveGrid`, and `Align` add responsive layout; `ThemeTokens` and scoped WPF `ThemeStyles` provide reusable button and input styling with interaction states. See [layout and styling](docs/layout-styling.md).

## Projects

- `UI Framework`: view descriptions, components, observable values/lists, bindings, dependency tracking.
- `UI Framework.Wpf`: native controls, scheduling, reconciliation, component lifetime.
- `samples/Counter`: project-workspace stress app and repeatable automated stress workload.
- `tests/UI Framework.Checks`: MSTest regression tests with STA execution for WPF, discoverable in Test Explorer.
- `docs/knowledge-graph.json`: maintained architecture nodes, relationships, status, and source evidence.
- `docs/knowledge-graph.md`: generated Mermaid map and source index.
- `docs/components.md`: component identity, lifecycle, and scheduling contracts.

Use `.Id(stableKey)` for dynamic siblings. Keys must be unique within a parent. Unkeyed children use their position as identity; changing kind, component type, or key replaces the instance. Removal releases the component; adding the same key later starts fresh state. Matching controls are updated in place. Spacing separates children; padding is inside a view's border.

## Knowledge graph

Open [the architecture map](docs/knowledge-graph.md). It connects APIs, state observation, rendering, tests, and outstanding limitations. It is a maintained design graph, not an automatic runtime trace or a vector database. Edit the JSON when architecture changes, then regenerate and validate:

```powershell
powershell -NoProfile -File tools/Update-KnowledgeGraph.ps1
powershell -NoProfile -File tools/Update-KnowledgeGraph.ps1 -Check
```

## Prototype limits

This is an initial working foundation, not a production SwiftUI replacement.

| Area | Current behavior |
| --- | --- |
| Component state | Retained per type and sibling identity; released on removal. |
| Lifecycle | Mount/unmount hooks; subtree subscriptions detach before cleanup hooks run. |
| Update scope | Local updates skip ancestors/siblings; explicit Memo inputs let unchanged child components skip parent-driven rebuilds. |
| Mutable collections | Structural changes observed through StateList; arbitrary object mutations are not deep-observed. |
| Binding | Direct/composed projections notify only when the selected value changes; raw State.Value reads still observe the whole state. |
| Styling | Theme tokens, scoped button/input styles, weighted rows, adaptive columns, alignment, and entry transitions; no general animation API. |
| Threading | State belongs to its creating thread; no concurrent state or automatic marshaling. |
| Focus | Controls, text, and selection retained in tested reorders; uninterrupted keyboard focus/IME behavior still needs visible-window testing. |
| Errors | Dependency tracking survives failed body builds; rendering is not transactional and has no error boundary/recovery UI. |
| Virtualization | Viewport controls plus a buffer; offscreen logical state retained; some large shuffles have quadratic cost. See [contracts](docs/virtualization.md). |
| Navigation | Typed history, Back/PopToRoot/Reset, logical screen snapshots, and WPF entry transitions. See [navigation](docs/navigation.md). |
| Still planned | General animation, advanced styling, hot reload integration, and non-Windows backends. |

For a demo already running in Debug, stop it before rebuilding, or use `dotnet run --project samples/Counter -c Release` to build and run separately.

